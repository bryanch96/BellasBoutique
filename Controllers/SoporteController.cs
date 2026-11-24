using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class SoporteController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Faqs.OrderBy(f => f.Orden).ToListAsync());

    public async Task<IActionResult> Chat()
    {
        var uid = User.UsuarioId();
        var ultimos = await db.MensajesChat
            .Where(m => m.ClienteId == uid)
            .OrderByDescending(m => m.Id)
            .Take(100)
            .ToListAsync();
        ultimos.Reverse();

        var mensajes = ultimos.Select(m => new MensajeVm
        {
            RemitenteId = m.RemitenteId,
            Nombre = m.EsStaff ? "Vendedor" : "Usted",
            Texto = m.Texto,
            Hora = m.Fecha.ToString("HH:mm"),
            EsStaff = m.EsStaff,
        }).ToList();

        return View(mensajes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarSugerencia(SugerenciaVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Escriba una sugerencia válida (máximo 1000 caracteres).";
            return RedirectToAction(nameof(Index));
        }

        db.Sugerencias.Add(new Sugerencia
        {
            UsuarioId = User.UsuarioId(),
            Mensaje = vm.Mensaje.Trim(),
            Fecha = DateTime.Now,
        });
        await db.SaveChangesAsync();

        TempData["Ok"] = "Gracias, su sugerencia fue enviada al administrador.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarEncuesta(EncuestaVm vm)
    {
        if (!ModelState.IsValid || vm.Calificacion < 1)
        {
            TempData["Error"] = "Seleccione una calificación del 1 al 5.";
            return RedirectToAction(nameof(Index));
        }

        db.Encuestas.Add(new Encuesta
        {
            UsuarioId = User.UsuarioId(),
            Calificacion = vm.Calificacion,
            Comentarios = vm.Comentarios?.Trim() ?? "",
            Fecha = DateTime.Now,
        });
        await db.SaveChangesAsync();

        TempData["Ok"] = "Gracias por su calificación.";
        return RedirectToAction(nameof(Index));
    }
}
