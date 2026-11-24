using System.Globalization;
using System.Text.Json;
using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Personal)]
public class GestionController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var ventas = await db.Ventas.Include(v => v.Producto).ToListAsync();
        var productos = await db.Productos.Where(p => p.Activo).ToListAsync();

        // Agregación en memoria: SQLite no suma decimales de forma nativa.
        var porProducto = ventas
            .GroupBy(v => v.ProductoId)
            .ToDictionary(g => g.Key, g => (Unidades: g.Sum(x => x.Cantidad), Ingresos: g.Sum(x => x.Cantidad * x.PrecioUnitario)));

        var todos = productos.Select(p =>
        {
            porProducto.TryGetValue(p.Id, out var s);
            return new ResumenProducto { ProductoId = p.Id, Nombre = p.Nombre, Unidades = s.Unidades, Ingresos = s.Ingresos };
        }).ToList();

        var ranking = todos.OrderByDescending(r => r.Unidades).ThenBy(r => r.Nombre).ToList();
        var top = ranking.Take(8).ToList();

        return View(new GestionDashboardVm
        {
            UnidadesTotales = todos.Sum(r => r.Unidades),
            IngresosTotales = todos.Sum(r => r.Ingresos),
            ProductosActivos = productos.Count,
            Ranking = ranking.Take(5).ToList(),
            MenosVendidos = todos.OrderBy(r => r.Unidades).ThenBy(r => r.Nombre).Take(5).ToList(),
            BajoStock = productos.Where(p => p.Stock <= Negocio.StockBajo).OrderBy(p => p.Stock).ToList(),
            GraficoLabels = JsonSerializer.Serialize(top.Select(r => r.Nombre)),
            GraficoDatos = JsonSerializer.Serialize(top.Select(r => r.Unidades)),
        });
    }

    public async Task<IActionResult> Reportes(string tipo = "diario", DateTime? fecha = null, int? anio = null, int? mes = null)
    {
        var cultura = new CultureInfo("es-CR");
        var hoy = DateTime.Today;
        DateTime inicio, fin, periodo;
        string titulo;

        if (tipo == "mensual")
        {
            inicio = new DateTime(anio ?? hoy.Year, mes ?? hoy.Month, 1);
            fin = inicio.AddMonths(1);
            periodo = inicio;
            titulo = "Reporte mensual";
        }
        else
        {
            tipo = "diario";
            inicio = (fecha ?? hoy).Date;
            fin = inicio.AddDays(1);
            periodo = inicio;
            titulo = "Reporte diario";
        }

        var ventas = await db.Ventas
            .Include(v => v.Producto)
            .Where(v => v.FechaVenta >= inicio && v.FechaVenta < fin)
            .ToListAsync();

        var filas = ventas
            .GroupBy(v => v.ProductoId)
            .Select(g => new ResumenProducto
            {
                ProductoId = g.Key,
                Nombre = g.First().Producto?.Nombre ?? "",
                Unidades = g.Sum(x => x.Cantidad),
                Ingresos = g.Sum(x => x.Cantidad * x.PrecioUnitario),
            })
            .OrderByDescending(r => r.Ingresos)
            .ToList();

        return View(new ReporteVm
        {
            Titulo = titulo,
            Tipo = tipo,
            Periodo = tipo == "mensual"
                ? periodo.ToString("MMMM yyyy", cultura)
                : periodo.ToString("dd/MM/yyyy"),
            Filas = filas,
            TotalUnidades = filas.Sum(f => f.Unidades),
            TotalIngresos = filas.Sum(f => f.Ingresos),
            BajoStock = await db.Productos
                .Where(p => p.Activo && p.Stock <= Negocio.StockBajo)
                .OrderBy(p => p.Stock)
                .ToListAsync(),
        });
    }
}
