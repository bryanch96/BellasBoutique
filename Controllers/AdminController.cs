using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Services;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class AdminController(AppDbContext db, BitacoraService bitacora) : Controller
{
    public async Task<IActionResult> Usuarios() =>
        View(await db.Usuarios.OrderBy(u => u.Nombre).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        if (id == User.UsuarioId())
        {
            TempData["Error"] = "No puede desactivar su propia cuenta.";
            return RedirectToAction(nameof(Usuarios));
        }

        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        u.Activo = !u.Activo;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(User.UsuarioId(), "Cambio de estado de cuenta",
            $"{u.Email}: {(u.Activo ? "activa" : "inactiva")}");
        TempData["Ok"] = "Estado de la cuenta actualizado.";
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, string rol)
    {
        if (rol is not (Roles.Cliente or Roles.Vendedor or Roles.Administrador))
        {
            TempData["Error"] = "Rol no válido.";
            return RedirectToAction(nameof(Usuarios));
        }
        if (id == User.UsuarioId())
        {
            TempData["Error"] = "No puede cambiar su propio rol.";
            return RedirectToAction(nameof(Usuarios));
        }

        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        var anterior = u.Rol;
        u.Rol = rol;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(User.UsuarioId(), "Cambio de rol", $"{u.Email}: {anterior} → {rol}");
        TempData["Ok"] = "Rol actualizado.";
        return RedirectToAction(nameof(Usuarios));
    }

    public async Task<IActionResult> Bitacora() =>
        View(await db.Bitacoras.Include(b => b.Usuario).OrderByDescending(b => b.Fecha).Take(300).ToListAsync());

    public async Task<IActionResult> Retroalimentacion()
    {
        var nombres = await db.Usuarios.ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);
        var encuestas = await db.Encuestas.OrderByDescending(e => e.Fecha).Take(100).ToListAsync();
        var sugerencias = await db.Sugerencias.OrderByDescending(s => s.Fecha).Take(100).ToListAsync();

        return View(new RetroalimentacionVm
        {
            Promedio = encuestas.Count == 0 ? 0 : Math.Round(encuestas.Average(e => e.Calificacion), 2),
            Encuestas = encuestas.Select(e => new RetroFila
            {
                Usuario = nombres.GetValueOrDefault(e.UsuarioId, "—"),
                Texto = e.Comentarios,
                Calificacion = e.Calificacion.ToString(),
                Fecha = e.Fecha.ToString("dd/MM/yyyy HH:mm"),
            }).ToList(),
            Sugerencias = sugerencias.Select(s => new RetroFila
            {
                Usuario = nombres.GetValueOrDefault(s.UsuarioId, "—"),
                Texto = s.Mensaje,
                Fecha = s.Fecha.ToString("dd/MM/yyyy HH:mm"),
            }).ToList(),
        });
    }
}
