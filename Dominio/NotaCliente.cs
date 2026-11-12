using System.ComponentModel.DataAnnotations;
using DKaiza.Web.Models;

namespace DKaiza.Web.Dominio;

public class NotaCliente
{
    public int Id { get; set; }

    // Cliente al que pertenece la nota
    public int ClienteId { get; set; }
    public Usuario? Cliente { get; set; }

    // Estilista que la escribió
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }

    [Required, MaxLength(500)]
    public string Texto { get; set; } = "";

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}