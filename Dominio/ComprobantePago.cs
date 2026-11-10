using System.ComponentModel.DataAnnotations;
using DKaiza.Web.Models;

namespace DKaiza.Web.Dominio;

public class ComprobantePago
{
    public int Id { get; set; }

    // Identificador único para trazabilidad (ej. B001-00000001)
    [Required, MaxLength(50)]
    public string NumeroComprobante { get; set; } = string.Empty;

    public int CitaId { get; set; }
    public Cita? Cita { get; set; }

    public int ClienteId { get; set; }
    public Usuario? Cliente { get; set; }

    // Datos del cliente asociados a la cita al momento de la emisión
    [Required, MaxLength(150)]
    public string ClienteNombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ClienteEmail { get; set; }

    [MaxLength(30)]
    public string? ClienteTelefono { get; set; }

    // Datos del servicio brindado
    [Required, MaxLength(150)]
    public string ServicioNombre { get; set; } = string.Empty;

    public int? EstilistaId { get; set; }

    [MaxLength(150)]
    public string? EstilistaNombre { get; set; }

    // Montos
    public decimal MontoBase { get; set; }
    public decimal Descuento { get; set; }

    [MaxLength(30)]
    public string? CuponCodigo { get; set; }

    public decimal MontoTotal { get; set; }

    // Método y trazabilidad de pago
    [Required, MaxLength(50)]
    public string MetodoPago { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NumeroOperacion { get; set; }

    // Fechas en UTC
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public DateTime FechaServicio { get; set; }
}

