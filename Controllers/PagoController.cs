using System.Text;
using System.Text.RegularExpressions;
using BellasBoutique.Data;
using BellasBoutique.Helpers;
using BellasBoutique.Models;
using BellasBoutique.Services;
using BellasBoutique.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class PagoController(AppDbContext db, BitacoraService bitacora, EmailOutbox outbox) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = User.UsuarioId();
        var carrito = await Carritos.CargarVistaAsync(db, uid);
        if (carrito.Vacio)
        {
            TempData["Error"] = "Su carrito está vacío.";
            return RedirectToAction("Index", "Carrito");
        }

        var usuario = await db.Usuarios.FindAsync(uid);
        return View(new PagoVm { Direccion = usuario?.Direccion ?? "", Carrito = carrito });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PagoVm vm)
    {
        var uid = User.UsuarioId();
        vm.Carrito = await Carritos.CargarVistaAsync(db, uid);
        if (vm.Carrito.Vacio)
        {
            TempData["Error"] = "Su carrito está vacío.";
            return RedirectToAction("Index", "Carrito");
        }

        if (!Metodos.Todos.Contains(vm.Metodo))
            ModelState.AddModelError(nameof(vm.Metodo), "Seleccione un método de pago.");

        if (vm.Metodo == Metodos.Tarjeta)
        {
            if (string.IsNullOrWhiteSpace(vm.NombreTarjeta))
                ModelState.AddModelError(nameof(vm.NombreTarjeta), "Ingrese el nombre de la tarjeta.");
            if (!ReglasPago.TarjetaValida(vm.NumeroTarjeta ?? ""))
                ModelState.AddModelError(nameof(vm.NumeroTarjeta), "Número de tarjeta inválido.");
            if (!ReglasPago.VencimientoValido(vm.Vencimiento ?? ""))
                ModelState.AddModelError(nameof(vm.Vencimiento), "Vencimiento inválido o vencido (MM/AA).");
            if (!Regex.IsMatch(vm.Cvv ?? "", "^\\d{3,4}$"))
                ModelState.AddModelError(nameof(vm.Cvv), "CVV inválido.");
        }
        if (!ModelState.IsValid) return View(vm);

        // Revalidación de existencias antes de cobrar.
        foreach (var i in vm.Carrito.Items)
        {
            if (i.Cantidad > i.Producto!.Stock)
            {
                ModelState.AddModelError(string.Empty,
                    $"Stock insuficiente para {i.Producto.Nombre}. Disponibles: {i.Producto.Stock}.");
                return View(vm);
            }
        }

        string estado;
        if (vm.Metodo == Metodos.Tarjeta)
            estado = PagoSimulador.ProcesarTarjeta(vm.NumeroTarjeta!);
        else
            estado = Estados.Pendiente; // SINPE y transferencia se confirman después

        var carritoId = vm.Carrito.CarritoId;
        using var tx = await db.Database.BeginTransactionAsync();

        var pago = new Pago
        {
            UsuarioId = uid,
            CarritoId = carritoId,
            Metodo = vm.Metodo,
            Estado = estado,
            Monto = vm.Carrito.Total,
            Fecha = DateTime.Now,
        };
        db.Pagos.Add(pago);
        await db.SaveChangesAsync();

        if (estado == Estados.Fallido)
        {
            await tx.CommitAsync();
            ViewData["ModalTitle"] = "Pago fallido";
            ViewData["ModalError"] = "No se pudo procesar el pago. Verifique los datos de la tarjeta e intente de nuevo.";
            return View(vm);
        }

        var usuario = await db.Usuarios.FindAsync(uid);
        var factura = new Factura
        {
            UsuarioId = uid,
            CarritoId = carritoId,
            PagoId = pago.Id,
            Fecha = DateTime.Now,
            Direccion = vm.Direccion.Trim(),
            Subtotal = vm.Carrito.Subtotal,
            Impuesto = vm.Carrito.Impuesto,
            Total = vm.Carrito.Total,
        };

        foreach (var i in vm.Carrito.Items)
        {
            factura.Detalles.Add(new FacturaDetalle
            {
                ProductoId = i.ProductoId,
                NombreProducto = i.Producto!.Nombre,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.Producto.Precio,
            });
            i.Producto.Stock -= i.Cantidad;
            db.Ventas.Add(new Venta
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.Producto.Precio,
                FechaVenta = DateTime.Now,
            });
        }
        db.Facturas.Add(factura);

        var carrito = await db.Carritos.FindAsync(carritoId);
        if (carrito is not null) carrito.Activo = false;

        await db.SaveChangesAsync();

        // Factura al correo del usuario y comprobante a la dirección de entrega (simulado).
        await outbox.EnviarAsync(usuario!.Email, $"Factura N.º {factura.Id:D6}", ResumenFactura(factura));
        await outbox.EnviarAsync(factura.Direccion, "Comprobante de entrega",
            $"Pedido {factura.Id:D6} para entregar en: {factura.Direccion}");
        await bitacora.RegistrarAsync(uid, "Compra", $"Factura {factura.Id:D6}, total {Money.Fmt(factura.Total)}");

        await tx.CommitAsync();
        return RedirectToAction(nameof(Confirmacion), new { id = factura.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmacion(int id)
    {
        var uid = User.UsuarioId();
        var personal = User.IsInRole(Roles.Vendedor) || User.IsInRole(Roles.Administrador);

        var f = await db.Facturas
            .Include(x => x.Detalles)
            .Include(x => x.Usuario)
            .Include(x => x.Pago)
            .FirstOrDefaultAsync(x => x.Id == id && (x.UsuarioId == uid || personal));

        return f is null ? NotFound() : View(f);
    }

    private static string ResumenFactura(Factura f)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Factura N.º {f.Id:D6} - {f.Fecha:dd/MM/yyyy}");
        foreach (var d in f.Detalles)
            sb.AppendLine($"{d.NombreProducto} x{d.Cantidad}: {Money.Fmt(d.PrecioUnitario * d.Cantidad)}");
        sb.AppendLine($"Subtotal: {Money.Fmt(f.Subtotal)}");
        sb.AppendLine($"Impuesto (13%): {Money.Fmt(f.Impuesto)}");
        sb.AppendLine($"Total: {Money.Fmt(f.Total)}");
        sb.AppendLine($"Entrega: {f.Direccion}");
        return sb.ToString();
    }
}
