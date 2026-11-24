using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class CarritoController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await Carritos.CargarVistaAsync(db, User.UsuarioId()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int itemId, int cantidad)
    {
        var uid = User.UsuarioId();
        var item = await db.CarritoItems
            .Include(i => i.Producto)
            .Include(i => i.Carrito)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Carrito!.UsuarioId == uid && i.Carrito.Activo);

        if (item is null) return NotFound();

        if (cantidad <= 0)
        {
            db.CarritoItems.Remove(item);
            TempData["Ok"] = "Producto quitado del carrito.";
        }
        else if (cantidad > item.Producto!.Stock)
        {
            TempData["Error"] = $"Stock insuficiente. Disponibles: {item.Producto.Stock}.";
        }
        else
        {
            item.Cantidad = cantidad;
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Quitar(int itemId) => Actualizar(itemId, 0);
}
