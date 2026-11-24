using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Services;

public class BitacoraService(AppDbContext db)
{
    public async Task RegistrarAsync(int? usuarioId, string accion, string detalle)
    {
        db.Bitacoras.Add(new Bitacora { UsuarioId = usuarioId, Accion = accion, Detalle = detalle, Fecha = DateTime.Now });
        await db.SaveChangesAsync();
    }
}

public class EmailOutbox(AppDbContext db)
{
    public async Task EnviarAsync(string destino, string asunto, string cuerpo)
    {
        db.EmailsEnviados.Add(new EmailEnviado { Destino = destino, Asunto = asunto, Cuerpo = cuerpo, Fecha = DateTime.Now });
        await db.SaveChangesAsync();
    }
}

public static class Carritos
{
    public static async Task<Carrito> ObtenerOCrearAsync(AppDbContext db, int usuarioId)
    {
        var carrito = await db.Carritos
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Activo);

        if (carrito is null)
        {
            carrito = new Carrito { UsuarioId = usuarioId, FechaCreacion = DateTime.Now, Activo = true };
            db.Carritos.Add(carrito);
            await db.SaveChangesAsync();
        }
        return carrito;
    }

    public static async Task<CarritoVm> CargarVistaAsync(AppDbContext db, int usuarioId)
    {
        var carrito = await db.Carritos
            .Include(c => c.Items).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Activo);

        var items = carrito?.Items.OrderBy(i => i.Id).ToList() ?? new List<CarritoItem>();
        decimal subtotal = items.Sum(i => i.Producto!.Precio * i.Cantidad);
        decimal impuesto = Negocio.CalcularImpuesto(subtotal);

        return new CarritoVm
        {
            CarritoId = carrito?.Id ?? 0,
            Items = items,
            Subtotal = subtotal,
            Impuesto = impuesto,
            Total = subtotal + impuesto,
        };
    }
}

// Simulador de pasarela: las tarjetas que terminan en 0002 son rechazadas.
// Tarjeta, SINPE y transferencia no guardan datos sensibles.
public static class PagoSimulador
{
    public static string ProcesarTarjeta(string numero) =>
        numero.Replace(" ", "").EndsWith("0002") ? Estados.Fallido : Estados.Exitoso;
}
