using DKaiza.Web.Dominio;
using Microsoft.EntityFrameworkCore;
using Usuario = DKaiza.Web.Models.Usuario;

namespace DKaiza.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CategoriaServicio> CategoriasServicio => Set<CategoriaServicio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Rol).HasConversion<int>();
        });

        modelBuilder.Entity<CategoriaServicio>(e =>
        {
            e.ToTable("CategoriasServicio");
        });

        modelBuilder.Entity<Servicio>(e =>
        {
            e.ToTable("Servicios", t =>
            {
                t.HasCheckConstraint("CK_Servicios_Duracion", "\"DuracionMinutos\" > 0");
                t.HasCheckConstraint("CK_Servicios_Precio", "\"Precio\" > 0");
            });
            e.Property(x => x.Precio).HasColumnType("numeric(10,2)");
            e.HasOne(s => s.Categoria)
             .WithMany(c => c.Servicios)
             .HasForeignKey(s => s.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}