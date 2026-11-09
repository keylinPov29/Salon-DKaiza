using DKaiza.Web.Models;

namespace DKaiza.Web.Dominio;

public class NotificacionCita
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public Cita? Cita { get; set; }
    public int ClienteId { get; set; }
    public Usuario? Cliente { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime? ResueltaEn { get; set; }
}
