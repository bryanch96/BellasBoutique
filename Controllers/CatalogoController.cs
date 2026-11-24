using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.Services;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize]
public class CatalogoController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? categoria, string? q)
    {
        var query = db.Productos.Include(p => p.Categoria).Where(p => p.Activo);

        if (categoria is not null)
            query = query.Where(p => p.CategoriaId == categoria);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var termino = q.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(termino));
        }

        var vm = new CatalogoVm
        {
            Productos = await query.OrderBy(p => p.Nombre).ToListAsync(),
            Categorias = await db.Categorias.OrderBy(c => c.Nombre).ToListAsync(),
            CategoriaId = categoria,
            Q = q,
        };
        return View(vm);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var p = await db.Productos.Include(x => x.Categoria).FirstOrDefaultAsync(x => x.Id == id && x.Activo);
        if (p is null) return NotFound();

        if (User.IsInRole(Roles.Cliente))
        {
            db.ProductosVistos.Add(new ProductoVisto { UsuarioId = User.UsuarioId(), ProductoId = id, Fecha = DateTime.Now });
            await db.SaveChangesAsync();
        }
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
    {
        if (!User.IsInRole(Roles.Cliente)) return Forbid();

        var p = await db.Productos.FindAsync(productoId);
        if (p is null || !p.Activo)
        {
            TempData["Error"] = "El producto no está disponible.";
            return RedirectToAction(nameof(Index));
        }

        if (cantidad < 1) cantidad = 1;

        var carrito = await Carritos.ObtenerOCrearAsync(db, User.UsuarioId());
        var item = carrito.Items.FirstOrDefault(i => i.ProductoId == productoId);
        int enCarrito = item?.Cantidad ?? 0;

        // Validación contra existencias antes de agregar.
        if (enCarrito + cantidad > p.Stock)
        {
            TempData["Error"] = $"Stock insuficiente. Disponibles: {Math.Max(0, p.Stock - enCarrito)}.";
            return RedirectToAction(nameof(Index));
        }

        if (item is null)
            db.CarritoItems.Add(new CarritoItem { CarritoId = carrito.Id, ProductoId = productoId, Cantidad = cantidad });
        else
            item.Cantidad += cantidad;

        // Cada vez que se agrega al carrito cuenta para "producto más cotizado".
        p.VecesAgregadoCarrito++;
        await db.SaveChangesAsync();

        TempData["Ok"] = $"{p.Nombre} se añadió al carrito.";
        return RedirectToAction(nameof(Index));
    }
}
