using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using Microsoft.AspNetCore.SignalR;

namespace BellasBoutique.Hubs;

// Chat en tiempo real. Cada cliente escucha su grupo "cliente-{id}";
// vendedores y administradores escuchan el grupo "personal".
public class ChatHub(AppDbContext db) : Hub
{
    private int UsuarioId => int.Parse(Context.UserIdentifier!);
    private bool EsPersonal =>
        Context.User!.IsInRole(Roles.Vendedor) || Context.User.IsInRole(Roles.Administrador);
    private string Nombre => Context.User!.Identity?.Name ?? "Usuario";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            Context.Abort();
            return;
        }

        var grupo = EsPersonal ? "personal" : $"cliente-{UsuarioId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
        await base.OnConnectedAsync();
    }

    public async Task EnviarMensaje(string texto, int? clienteId)
    {
        texto = (texto ?? "").Trim();
        if (texto.Length == 0 || texto.Length > 1000) return;

        int clienteDestino;
        if (EsPersonal)
        {
            if (clienteId is null) return;
            clienteDestino = clienteId.Value;
        }
        else
        {
            clienteDestino = UsuarioId;
        }

        var msg = new MensajeChat
        {
            ClienteId = clienteDestino,
            RemitenteId = UsuarioId,
            EsStaff = EsPersonal,
            Texto = texto,
            Fecha = DateTime.Now,
        };
        db.MensajesChat.Add(msg);
        await db.SaveChangesAsync();

        var payload = new
        {
            clienteId = clienteDestino,
            remitenteId = UsuarioId,
            nombre = Nombre,
            esStaff = EsPersonal,
            texto,
            fecha = msg.Fecha.ToString("HH:mm"),
        };

        await Clients.Group($"cliente-{clienteDestino}").SendAsync("RecibirMensaje", payload);
        await Clients.Group("personal").SendAsync("RecibirMensaje", payload);
    }
}
