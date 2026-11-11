namespace DKaiza.Web.Models;

/// <summary>
/// Modelo para la vista y emisión del comprobante digital (ticket) de venta.
/// </summary>
public class ComprobanteViewModel
{
    public int Id { get; set; }

    // Identificador único de trazabilidad
    public string Serie { get; set; } = "B001";
    public int CitaId { get; set; }
    private string? _numeroComprobante;
    public string NumeroComprobante
    {
        get => !string.IsNullOrWhiteSpace(_numeroComprobante) ? _numeroComprobante : $"{Serie}-{CitaId:D8}";
        set => _numeroComprobante = value;
    }
    public string? NumeroOperacion { get; set; }

    // Fechas (en hora local del salón)
    public DateTime FechaEmision { get; set; }
    public DateTime FechaCitaInicio { get; set; }
    public DateTime FechaCitaFin { get; set; }

    // Datos del emisor
    public string EmpresaNombre { get; set; } = "Salón de Belleza D'Kaiza S.A.C.";
    public string EmpresaRuc { get; set; } = "20608945123";
    public string EmpresaDireccion { get; set; } = "Av. Principal 123, Lima, Perú";
    public string EmpresaTelefono { get; set; } = "+51 999 888 777";
    public string EmpresaEmail { get; set; } = "contacto@dkaiza.com";

    // Datos del cliente
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteEmail { get; set; } = "";
    public string ClienteTelefono { get; set; } = "";

    // Servicio brindado
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public string? CategoriaNombre { get; set; }
    public int DuracionMinutos { get; set; }
    public string EstilistaNombre { get; set; } = "";

    // Desglose económico
    public decimal PrecioBase { get; set; }
    public decimal Descuento { get; set; }
    public string? CuponCodigo { get; set; }
    public decimal TotalPagado { get; set; }

    // Cálculo tributario referencial (Perú - IGV 18%)
    public decimal SubtotalGravado => Math.Round(TotalPagado / 1.18m, 2);
    public decimal MontoIgv => TotalPagado - SubtotalGravado;

    // Método y estado del pago
    public string MetodoPago { get; set; } = "";
    public string EstadoPago { get; set; } = "PAGADO";

    // Hash o código digital de verificación
    public string CodigoVerificacion => $"{NumeroComprobante}|{TotalPagado:F2}|{FechaEmision:yyyyMMddHHmmss}";
}
