using DKaiza.Web.Dominio;

namespace DKaiza.Web.Services;

public interface IComprobanteService
{
    /// <summary>
    /// Genera automáticamente el comprobante de venta digital para una cita cuyo pago haya sido registrado.
    /// Si el comprobante ya existe, devuelve el existente.
    /// </summary>
    Task<ComprobantePago?> GenerarComprobanteAsync(int citaId);

    /// <summary>
    /// Obtiene el comprobante de pago por el ID de la cita. Si la cita está pagada y aún no lo tiene, lo genera.
    /// Si clienteId es provisto, valida que el comprobante pertenezca a ese cliente.
    /// </summary>
    Task<ComprobantePago?> ObtenerPorCitaIdAsync(int citaId, int? clienteId = null);

    /// <summary>
    /// Obtiene el comprobante de pago por su ID.
    /// Si clienteId es provisto, valida que el comprobante pertenezca a ese cliente.
    /// </summary>
    Task<ComprobantePago?> ObtenerPorIdAsync(int id, int? clienteId = null);

    /// <summary>
    /// Obtiene el comprobante formateado en ViewModel para su visualización/descarga/impresión.
    /// </summary>
    Task<DKaiza.Web.Models.ComprobanteViewModel?> ObtenerViewModelPorCitaIdAsync(int citaId, int? clienteId = null);

    /// <summary>
    /// Obtiene el comprobante formateado en ViewModel para su visualización/descarga/impresión.
    /// </summary>
    Task<DKaiza.Web.Models.ComprobanteViewModel?> ObtenerViewModelPorIdAsync(int id, int? clienteId = null);
}
