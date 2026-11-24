using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.Services;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Personal)]
public class ProductosController(AppDbContext db, BitacoraService bitacora) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Productos.Include(p => p.Categoria).Include(p => p.Proveedor).OrderBy(p => p.Nombre).ToListAsync());

    public async Task<IActionResult> Inventario() =>
        View(await db.Productos.Where(p => p.Activo).OrderBy(p => p.Stock).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View("Form", new ProductoFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductoFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View("Form", vm);
        }

        var p = new Producto
        {
            Nombre = vm.Nombre.Trim(),
            Descripcion = vm.Descripcion.Trim(),
            Precio = vm.Precio,
            Stock = vm.Stock,
            ImagenUrl = string.IsNullOrWhiteSpace(vm.ImagenUrl) ? null : vm.ImagenUrl.Trim(),
            CategoriaId = vm.CategoriaId,
            ProveedorId = vm.ProveedorId,
            Activo = true,
        };
        db.Productos.Add(p);
        await db.SaveChangesAsync();

        await bitacora.RegistrarAsync(User.UsuarioId(), "Alta de producto", p.Nombre);
        TempData["Ok"] = "Producto creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        await CargarListasAsync();
        return View("Form", new ProductoFormVm
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            Stock = p.Stock,
            ImagenUrl = p.ImagenUrl,
            CategoriaId = p.CategoriaId,
            ProveedorId = p.ProveedorId,
            Activo = p.Activo,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductoFormVm vm)
    {
        var p = await db.Productos.FindAsync(vm.Id);
        if (p is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View("Form", vm);
        }

        p.Nombre = vm.Nombre.Trim();
        p.Descripcion = vm.Descripcion.Trim();
        p.Precio = vm.Precio;
        p.Stock = vm.Stock;
        p.ImagenUrl = string.IsNullOrWhiteSpace(vm.ImagenUrl) ? null : vm.ImagenUrl.Trim();
        p.CategoriaId = vm.CategoriaId;
        p.ProveedorId = vm.ProveedorId;
        p.Activo = vm.Activo;
        await db.SaveChangesAsync();

        await bitacora.RegistrarAsync(User.UsuarioId(), "Modificación de producto", p.Nombre);
        TempData["Ok"] = "Cambios guardados.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        p.Activo = false;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(User.UsuarioId(), "Baja de producto", p.Nombre);
        TempData["Ok"] = "Producto desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        p.Activo = true;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(User.UsuarioId(), "Activación de producto", p.Nombre);
        TempData["Ok"] = "Producto activado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarStock(int id, int stock)
    {
        var p = await db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        if (stock < 0)
        {
            TempData["Error"] = "El stock no puede ser negativo.";
            return RedirectToAction(nameof(Inventario));
        }

        var anterior = p.Stock;
        p.Stock = stock;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(User.UsuarioId(), "Actualización de stock", $"{p.Nombre}: {anterior} → {stock}");
        TempData["Ok"] = $"Stock de {p.Nombre} actualizado.";
        return RedirectToAction(nameof(Inventario));
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Categorias = new SelectList(await db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Proveedores = new SelectList(await db.Proveedores.OrderBy(p => p.Nombre).ToListAsync(), "Id", "Nombre");
    }
}
