using System.Security.Claims;
using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.Services;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

public class AccountController(
    AppDbContext db,
    IPasswordHasher<Usuario> hasher,
    BitacoraService bitacora,
    EmailOutbox outbox) : Controller
{
    [HttpGet]
    public IActionResult Login(string? timeout)
    {
        if (User.Identity?.IsAuthenticated == true)
            return User.IsInRole(Roles.Cliente)
                ? RedirectToAction("Dashboard", "Cliente")
                : RedirectToAction("Dashboard", "Gestion");

        ViewData["Timeout"] = timeout == "1";
        return View(new LoginVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        bool ok = u is not null && u.Activo
            && hasher.VerifyHashedPassword(u, u.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;

        // Cada intento queda registrado en IntentosLogin.
        db.IntentosLogin.Add(new IntentoLogin { Email = email, UsuarioId = u?.Id, Exitoso = ok, Fecha = DateTime.Now });
        await db.SaveChangesAsync();

        if (!ok || u is null)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos, o la cuenta está inactiva.");
            return View(vm);
        }

        await IniciarSesionAsync(u);
        await bitacora.RegistrarAsync(u.Id, "Inicio de sesión", u.Email);

        return u.Rol == Roles.Cliente
            ? RedirectToAction("Dashboard", "Cliente")
            : RedirectToAction("Dashboard", "Gestion");
    }

    [HttpGet]
    public IActionResult Registro() => View(new RegistroVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var reglaPass = ReglasPassword.Validar(vm.Password);
        if (reglaPass is not null)
        {
            ModelState.AddModelError(nameof(vm.Password), reglaPass);
            return View(vm);
        }

        var email = vm.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(x => x.Email == email))
        {
            ViewData["ModalError"] = "El correo electrónico ya se encuentra registrado.";
            return View(vm);
        }
        var cedula = vm.Cedula.Trim();
        if (await db.Usuarios.AnyAsync(x => x.Cedula == cedula))
        {
            ViewData["ModalError"] = "La cédula ya se encuentra registrada.";
            return View(vm);
        }

        var nuevo = new Usuario
        {
            Cedula = cedula,
            Nombre = vm.Nombre.Trim(),
            Apellidos = vm.Apellidos.Trim(),
            Email = email,
            Telefono = vm.Telefono.Trim(),
            Direccion = vm.Direccion.Trim(),
            Rol = Roles.Cliente,
            FechaRegistro = DateTime.Now,
        };
        nuevo.PasswordHash = hasher.HashPassword(nuevo, vm.Password);
        db.Usuarios.Add(nuevo);
        await db.SaveChangesAsync();

        await bitacora.RegistrarAsync(nuevo.Id, "Alta de usuario", nuevo.Email);
        TempData["Ok"] = "Cuenta creada. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Recuperar() => View(new RecuperarVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recuperar(RecuperarVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null)
        {
            ViewData["Error"] = "El correo electrónico no está registrado.";
            return View(vm);
        }

        // Según el wireframe: se genera una nueva contraseña automáticamente y se envía por correo.
        var nueva = ReglasPassword.Generar();
        u.PasswordHash = hasher.HashPassword(u, nueva);
        await db.SaveChangesAsync();

        await outbox.EnviarAsync(u.Email, "Bellas Boutique: nueva contraseña",
            $"Su nueva contraseña temporal es: {nueva}. Le recomendamos cambiarla al iniciar sesión.");
        await bitacora.RegistrarAsync(u.Id, "Recuperación de contraseña", u.Email);

        TempData["Ok"] = "Le enviamos una nueva contraseña a su correo electrónico.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        if (u is null) return RedirectToAction(nameof(Login));

        return View(new PerfilVm
        {
            IdVisible = u.IdVisible,
            Email = u.Email,
            Nombre = u.Nombre,
            Apellidos = u.Apellidos,
            Telefono = u.Telefono,
            Direccion = u.Direccion,
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilVm vm)
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        if (u is null) return RedirectToAction(nameof(Login));

        // ID y correo no se pueden modificar.
        vm.IdVisible = u.IdVisible;
        vm.Email = u.Email;
        if (!ModelState.IsValid) return View(vm);

        u.Nombre = vm.Nombre.Trim();
        u.Apellidos = vm.Apellidos.Trim();
        u.Telefono = vm.Telefono.Trim();
        u.Direccion = vm.Direccion.Trim();
        await db.SaveChangesAsync();

        await bitacora.RegistrarAsync(u.Id, "Modificación de perfil", u.Email);
        TempData["Ok"] = "Cambios guardados.";
        return RedirectToAction(nameof(Perfil));
    }

    // Sin [Authorize] a propósito: si la sesión ya expiró por inactividad, igual debe poder cerrarse.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? timeout)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is not null)
        {
            await bitacora.RegistrarAsync(int.Parse(idClaim), "Cierre de sesión",
                timeout == "1" ? "Por inactividad" : "Manual");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login), new { timeout });
    }

    [Authorize, HttpGet]
    public IActionResult Ping() => NoContent();

    private async Task IniciarSesionAsync(Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.NombreCompleto),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.Rol),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
    }
}
