using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Personal)]
public class ChatController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? clienteId)
    {
        var mensajes = await db.MensajesChat.OrderByDescending(m => m.Fecha).ToListAsync();
        var nombres = await db.Usuarios.ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);

        var conversaciones = mensajes
            .GroupBy(m => m.ClienteId)
            .Select(g =>
            {
                var ultimo = g.First();
                return new ConversacionVm
                {
                    ClienteId = g.Key,
                    Nombre = nombres.GetValueOrDefault(g.Key, "Cliente"),
                    Ultimo = ultimo.Texto.Length > 40 ? ultimo.Texto[..40] + "…" : ultimo.Texto,
                    Hora = ultimo.Fecha.ToString("HH:mm"),
                };
            })
            .ToList();

        var vm = new ChatStaffVm
        {
            Conversaciones = conversaciones,
            ClienteId = clienteId ?? conversaciones.FirstOrDefault()?.ClienteId,
        };

        if (vm.ClienteId is int cid)
        {
            vm.ClienteNombre = nombres.GetValueOrDefault(cid, "Cliente");
            vm.Mensajes = mensajes
                .Where(m => m.ClienteId == cid)
                .OrderBy(m => m.Fecha)
                .TakeLast(100)
                .Select(m => new MensajeVm
                {
                    RemitenteId = m.RemitenteId,
                    Nombre = nombres.GetValueOrDefault(m.RemitenteId, "Usuario"),
                    Texto = m.Texto,
                    Hora = m.Fecha.ToString("HH:mm"),
                    EsStaff = m.EsStaff,
                })
                .ToList();
        }

        return View(vm);
    }
}
