using DKaiza.Web.Dominio;

namespace DKaiza.Web.Models;

public class RecepcionistaViewModel
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public string? Buscar { get; set; }
    public int PagosPendientesCount { get; set; }
    public decimal TotalPendiente { get; set; }
    public decimal TotalCobradoHoy { get; set; }
    public List<PagoRecepcionDto> Pendientes { get; set; } = new();
    public List<PagoRecepcionDto> Historial { get; set; } = new();
}

public class PagoRecepcionDto
{
    public int CitaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Servicio { get; set; } = "";
    public string Estilista { get; set; } = "";
    public DateTime Inicio { get; set; }
    public decimal Monto { get; set; }
    public MetodoPagoCita? MetodoPago { get; set; }
    public EstadoCita Estado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? CuponCodigo { get; set; }
}
