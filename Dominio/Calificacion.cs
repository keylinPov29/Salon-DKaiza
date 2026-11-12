using System.ComponentModel.DataAnnotations;
using DKaiza.Web.Models;
 
namespace DKaiza.Web.Dominio;
 
/// <summary>
/// HU-14: calificación de un servicio y de la estilista que lo atendió.
/// Solo se puede crear sobre una cita Completada y una única vez por cita.
/// </summary>
public class Calificacion
{
    public int Id { get; set; }
 
    // Cita calificada (relación 1 a 1: una cita solo se califica una vez)
    public int CitaId { get; set; }
    public Cita? Cita { get; set; }
 
    // Estilista calificada (se guarda aparte para calcular promedios sin unir con Citas)
    public int EstilistaId { get; set; }
    public Estilista? Estilista { get; set; }
 
    // Cliente que califica
    public int ClienteId { get; set; }
    public Usuario? Cliente { get; set; }
 
    // De 1 a 5
    public int Estrellas { get; set; }
 
    [MaxLength(250)]
    public string? Comentario { get; set; }
 
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
 