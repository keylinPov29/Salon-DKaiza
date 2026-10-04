using DKaiza.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Data;

public static class AdminSeeder
{
    public static async Task SembrarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        if (await db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Administrador)) return;

        var email = cfg["AdminInicial:Email"];
        var pass = cfg["AdminInicial:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass)) return;

        db.Usuarios.Add(new Usuario
        {
            Nombre = "Administrador",
            Apellido = "D'Kaiza",
            Email = email.Trim(),
            Telefono = "999999999",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass),
            Rol = RolUsuario.Administrador,
            DebeCambiarPassword = true
        });
        await db.SaveChangesAsync();
    }
}