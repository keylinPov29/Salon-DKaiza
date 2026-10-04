using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Dominio;

public class Servicio
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }
    public CategoriaServicio? Categoria { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }

    public byte[]? Imagen { get; set; }

    [MaxLength(20)]
    public string? ImagenTipo { get; set; }

    public bool Activo { get; set; } = true;

    // Auditoría
    public int UsuarioRegistroId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public int? UsuarioModificaId { get; set; }
    public DateTime? FechaModifica { get; set; }
}