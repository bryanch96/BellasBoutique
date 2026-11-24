using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class ClienteController(AppDbContext db, IConfiguration cfg) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var uid = User.UsuarioId();

        var masCotizados = await db.Productos
            .Where(p => p.Activo)
            .OrderByDescending(p => p.VecesAgregadoCarrito)
            .Take(4)
            .ToListAsync();

        var vistos = await db.ProductosVistos
            .Include(v => v.Producto)
            .Where(v => v.UsuarioId == uid && v.Producto!.Activo)
            .OrderByDescending(v => v.Fecha)
            .Take(30)
            .ToListAsync();

        var encuestas = await db.Encuestas.Select(e => e.Calificacion).ToListAsync();
        int satisfaccion = encuestas.Count == 0
            ? cfg.GetValue<int>("Marca:SatisfaccionBase")
            : (int)Math.Round(encuestas.Average() / 5.0 * 100);

        return View(new ClienteDashboardVm
        {
            MasCotizados = masCotizados,
            Vistos = vistos.Select(v => v.Producto!).DistinctBy(p => p.Id).Take(4).ToList(),
            Anios = cfg.GetValue<int>("Marca:AnosExperiencia"),
            Clientes = cfg.GetValue<int>("Marca:ClientesFelices"),
            Paises = cfg.GetValue<int>("Marca:PaisesAlcanzados"),
            Satisfaccion = satisfaccion,
        });
    }
}
