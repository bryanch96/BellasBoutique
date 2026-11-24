using System.ComponentModel.DataAnnotations;
using BellasBoutique.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BellasBoutique.ViewModels;

public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class RegistroVm
{
    [Required, StringLength(20)] public string Cedula { get; set; } = "";
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [Required, StringLength(120)] public string Apellidos { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(20)] public string Telefono { get; set; } = "";
    [Required, StringLength(200)] public string Direccion { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = "";
}

public class RecuperarVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}

public class PerfilVm
{
    public string IdVisible { get; set; } = "";
    public string Email { get; set; } = "";
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [Required, StringLength(120)] public string Apellidos { get; set; } = "";
    [Required, StringLength(20)] public string Telefono { get; set; } = "";
    [Required, StringLength(200)] public string Direccion { get; set; } = "";
}

public class CatalogoVm
{
    public List<Producto> Productos { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = new();
    public int? CategoriaId { get; set; }
    public string? Q { get; set; }
}

public class CarritoVm
{
    public int CarritoId { get; set; }
    public List<CarritoItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public bool Vacio => Items.Count == 0;
}

public class PagoVm
{
    [Required] public string Direccion { get; set; } = "";
    [Required] public string Metodo { get; set; } = "Tarjeta";
    public string? NombreTarjeta { get; set; }
    public string? NumeroTarjeta { get; set; }
    public string? Vencimiento { get; set; }
    public string? Cvv { get; set; }

    [BindNever] public CarritoVm Carrito { get; set; } = new();
}

public class ResumenProducto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public int Unidades { get; set; }
    public decimal Ingresos { get; set; }
}

public class GestionDashboardVm
{
    public int UnidadesTotales { get; set; }
    public decimal IngresosTotales { get; set; }
    public int ProductosActivos { get; set; }
    public List<ResumenProducto> Ranking { get; set; } = new();
    public List<ResumenProducto> MenosVendidos { get; set; } = new();
    public List<Producto> BajoStock { get; set; } = new();
    public string GraficoLabels { get; set; } = "[]";
    public string GraficoDatos { get; set; } = "[]";
}

public class ReporteVm
{
    public string Titulo { get; set; } = "";
    public string Periodo { get; set; } = "";
    public string Tipo { get; set; } = "diario";
    public List<ResumenProducto> Filas { get; set; } = new();
    public int TotalUnidades { get; set; }
    public decimal TotalIngresos { get; set; }
    public List<Producto> BajoStock { get; set; } = new();
}

public class ProductoFormVm
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [StringLength(500)] public string Descripcion { get; set; } = "";
    [Range(1, 100000000, ErrorMessage = "El precio debe ser mayor a cero.")] public decimal Precio { get; set; }
    [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo.")] public int Stock { get; set; }
    public string? ImagenUrl { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")] public int CategoriaId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un proveedor.")] public int ProveedorId { get; set; }
    public bool Activo { get; set; } = true;
}

public class ClienteDashboardVm
{
    public List<Producto> MasCotizados { get; set; } = new();
    public List<Producto> Vistos { get; set; } = new();
    public int Anios { get; set; }
    public int Clientes { get; set; }
    public int Paises { get; set; }
    public int Satisfaccion { get; set; }
}

public class SugerenciaVm
{
    [Required, StringLength(1000)] public string Mensaje { get; set; } = "";
}

public class EncuestaVm
{
    [Range(1, 5)] public int Calificacion { get; set; }
    [StringLength(500)] public string? Comentarios { get; set; }
}

public class MensajeVm
{
    public int RemitenteId { get; set; }
    public string Nombre { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Hora { get; set; } = "";
    public bool EsStaff { get; set; }
}

public class ConversacionVm
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = "";
    public string Ultimo { get; set; } = "";
    public string Hora { get; set; } = "";
}

public class ChatStaffVm
{
    public List<ConversacionVm> Conversaciones { get; set; } = new();
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public List<MensajeVm> Mensajes { get; set; } = new();
}

public class RetroFila
{
    public string Usuario { get; set; } = "";
    public string Texto { get; set; } = "";
    public string? Calificacion { get; set; }
    public string Fecha { get; set; } = "";
}

public class RetroalimentacionVm
{
    public double Promedio { get; set; }
    public List<RetroFila> Encuestas { get; set; } = new();
    public List<RetroFila> Sugerencias { get; set; } = new();
}
