using DKaiza.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Rol).HasConversion<int>();
        });
    }
}