namespace BellasBoutique.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Cedula { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "Cliente";
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string IdVisible => Id.ToString("D6");
    public string NombreCompleto => $"{Nombre} {Apellidos}";
}

public class IntentoLogin
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public int? UsuarioId { get; set; }
    public bool Exitoso { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;
    // Métrica de "producto más cotizado": veces que se agregó al carrito.
    public int VecesAgregadoCarrito { get; set; }
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
}

public class Carrito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public bool Activo { get; set; } = true;
    public List<CarritoItem> Items { get; set; } = new();
}

public class CarritoItem
{
    public int Id { get; set; }
    public int CarritoId { get; set; }
    public Carrito? Carrito { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
}

public class Pago
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int CarritoId { get; set; }
    public string Metodo { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class Factura
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int CarritoId { get; set; }
    public int PagoId { get; set; }
    public Pago? Pago { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Direccion { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public List<FacturaDetalle> Detalles { get; set; } = new();
}

public class FacturaDetalle
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class Venta
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public DateTime FechaVenta { get; set; } = DateTime.Now;
}

public class Encuesta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Calificacion { get; set; }
    public string Comentarios { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class Sugerencia
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Mensaje { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class Faq
{
    public int Id { get; set; }
    public string Pregunta { get; set; } = "";
    public string Respuesta { get; set; } = "";
    public int Orden { get; set; }
}

public class MensajeChat
{
    public int Id { get; set; }
    // Cliente dueño de la conversación.
    public int ClienteId { get; set; }
    public int RemitenteId { get; set; }
    public bool EsStaff { get; set; }
    public string Texto { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class Bitacora
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Accion { get; set; } = "";
    public string Detalle { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class ProductoVisto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

// Bandeja de salida: simula el envío de correos (se consulta en la tabla EmailsEnviados).
public class EmailEnviado
{
    public int Id { get; set; }
    public string Destino { get; set; } = "";
    public string Asunto { get; set; } = "";
    public string Cuerpo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
}
