using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Dominio;

public class CategoriaServicio
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public byte[]? Imagen { get; set; }

    [MaxLength(20)]
    public string? ImagenTipo { get; set; }

    public bool Activo { get; set; } = true;

    // Auditoría
    public int UsuarioRegistroId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public int? UsuarioModificaId { get; set; }
    public DateTime? FechaModifica { get; set; }

    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}