using BellasBoutique.Models;
using Microsoft.EntityFrameworkCore;

namespace BellasBoutique.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<IntentoLogin> IntentosLogin => Set<IntentoLogin>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Carrito> Carritos => Set<Carrito>();
    public DbSet<CarritoItem> CarritoItems => Set<CarritoItem>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Encuesta> Encuestas => Set<Encuesta>();
    public DbSet<Sugerencia> Sugerencias => Set<Sugerencia>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<MensajeChat> MensajesChat => Set<MensajeChat>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<ProductoVisto> ProductosVistos => Set<ProductoVisto>();
    public DbSet<EmailEnviado> EmailsEnviados => Set<EmailEnviado>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.Cedula).IsUnique();
    }
}
