using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Models;

public enum RolUsuario
{
    Cliente = 1,
    Estilista = 2,
    Recepcionista = 3,
    Administrador = 4
}

public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required, StringLength(100)]
    public string Apellido { get; set; } = "";

    [Required, StringLength(150), EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(20)]
    public string Telefono { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public RolUsuario Rol { get; set; } = RolUsuario.Cliente;
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public string NombreCompleto => $"{Nombre} {Apellido}";
}