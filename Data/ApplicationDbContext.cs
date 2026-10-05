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
    public DbSet<Estilista> Estilistas => Set<Estilista>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Rol).HasConversion<int>();

            // Cuenta de acceso vinculada a la ficha de estilista (si se borra la ficha, queda en NULL)
            e.HasOne<Estilista>()
             .WithMany()
             .HasForeignKey(u => u.EstilistaId)
             .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<Estilista>(e =>
        {
            e.ToTable("Estilistas");
            e.HasIndex(x => x.Dni).IsUnique();

            var fecha = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc);

            // Horario real del salón: 9:30–21:00, con cierre al mediodía de 13:00 a 15:00
            e.HasData(
                new Estilista
                {
                    Id = 1, Nombres = "María", Apellidos = "González", Dni = "70000001",
                    Telefono = "999000001", Especialidad = "Coloración",
                    InicioJornada = new TimeOnly(9, 30), FinJornada = new TimeOnly(21, 0),
                    InicioDescanso = new TimeOnly(13, 0), FinDescanso = new TimeOnly(15, 0),
                    Activo = true, FechaRegistro = fecha
                },
                new Estilista
                {
                    Id = 2, Nombres = "Laura", Apellidos = "Fernández", Dni = "70000002",
                    Telefono = "999000002", Especialidad = "Cabello",
                    InicioJornada = new TimeOnly(9, 30), FinJornada = new TimeOnly(21, 0),
                    InicioDescanso = new TimeOnly(13, 0), FinDescanso = new TimeOnly(15, 0),
                    Activo = true, FechaRegistro = fecha
                },
                new Estilista
                {
                    Id = 3, Nombres = "Carolina", Apellidos = "Rojas", Dni = "70000003",
                    Telefono = "999000003", Especialidad = "Tratamientos",
                    InicioJornada = new TimeOnly(9, 30), FinJornada = new TimeOnly(21, 0),
                    InicioDescanso = new TimeOnly(13, 0), FinDescanso = new TimeOnly(15, 0),
                    Activo = true, FechaRegistro = fecha
                });
        });
    }
}