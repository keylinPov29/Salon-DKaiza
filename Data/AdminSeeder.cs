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
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        await db.Database.MigrateAsync();

        if (!await db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Administrador))
        {
            var adminEmail = cfg["AdminInicial:Email"];
            var adminPassword = cfg["AdminInicial:Password"];
            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                db.Usuarios.Add(new Usuario
                {
                    Nombre = "Administrador",
                    Apellido = "D'Kaiza",
                    Email = adminEmail.Trim(),
                    Telefono = "999999999",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Rol = RolUsuario.Administrador,
                    DebeCambiarPassword = true
                });
            }
        }

        if (environment.IsDevelopment() &&
            !await db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Recepcionista))
        {
            var receptionistEmail = cfg["RecepcionistaInicial:Email"];
            if (string.IsNullOrWhiteSpace(receptionistEmail))
                receptionistEmail = "recepcionista@dkaiza.local";
            var receptionistPassword = cfg["RecepcionistaInicial:Password"];
            if (!string.IsNullOrWhiteSpace(receptionistPassword))
            {
                var emailEnUso = await db.Usuarios.AnyAsync(u =>
                    u.Email.ToLower() == receptionistEmail.Trim().ToLower());

                if (!emailEnUso)
                {
                    db.Usuarios.Add(new Usuario
                    {
                        Nombre = "Recepcionista",
                        Apellido = "D'Kaiza",
                        Email = receptionistEmail.Trim(),
                        Telefono = "999999998",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(receptionistPassword),
                        Rol = RolUsuario.Recepcionista,
                        DebeCambiarPassword = true
                    });
                }
                else
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(nameof(AdminSeeder));
                    logger.LogWarning(
                        "No se creó el usuario de recepción porque el correo configurado ya está registrado.");
                }
            }
        }

        await db.SaveChangesAsync();
    }
}