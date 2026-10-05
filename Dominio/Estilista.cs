using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Dominio;

public class Estilista
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, MaxLength(12)]
    public string Dni { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Especialidad { get; set; } = string.Empty;

    public TimeOnly InicioJornada { get; set; } = new(10, 0);
    public TimeOnly FinJornada { get; set; } = new(22, 0);
    public TimeOnly? InicioDescanso { get; set; }
    public TimeOnly? FinDescanso { get; set; }

    public byte[]? Foto { get; set; }

    [MaxLength(20)]
    public string? FotoTipo { get; set; }

    public bool Activo { get; set; } = true;

    public int? UsuarioRegistroId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public int? UsuarioModificaId { get; set; }
    public DateTime? FechaModifica { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}