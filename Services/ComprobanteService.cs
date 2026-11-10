using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Services;

public class ComprobanteService : IComprobanteService
{
    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly ApplicationDbContext _db;
    private readonly ILogger<ComprobanteService> _logger;

    public ComprobanteService(ApplicationDbContext db, ILogger<ComprobanteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ComprobantePago?> GenerarComprobanteAsync(int citaId)
    {
        // 1. Verificar si ya existe comprobante para esta cita (idempotencia)
        var existente = await _db.ComprobantesPago
            .Include(c => c.Cita)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.CitaId == citaId);

        if (existente != null)
        {
            return existente;
        }

        // 2. Obtener cita completa
        var cita = await _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == citaId);

        if (cita == null)
        {
            _logger.LogWarning("Intento de generar comprobante para cita inexistente {CitaId}", citaId);
            return null;
        }

        // Criterio HU: El comprobante digital solo debe generarse cuando el pago de la cita se haya registrado correctamente.
        var pagoRegistrado = cita.Estado == EstadoCita.Pagada || cita.FechaPago != null;
        if (!pagoRegistrado)
        {
            _logger.LogWarning("No se puede generar comprobante para cita {CitaId} en estado {Estado} sin pago registrado", citaId, cita.Estado);
            return null;
        }

        // 3. Generar identificador único correlativo (ej. B001-00000001)
        var ultimoId = await _db.ComprobantesPago.MaxAsync(c => (int?)c.Id) ?? 0;
        var correlativo = ultimoId + 1;
        var numeroComprobante = $"B001-{correlativo:D8}";

        while (await _db.ComprobantesPago.AnyAsync(c => c.NumeroComprobante == numeroComprobante))
        {
            correlativo++;
            numeroComprobante = $"B001-{correlativo:D8}";
        }

        // 4. Instanciar comprobante con datos congelados de la transacción
        var comprobante = new ComprobantePago
        {
            NumeroComprobante = numeroComprobante,
            CitaId = cita.Id,
            ClienteId = cita.ClienteId,
            ClienteNombre = cita.Cliente?.NombreCompleto ?? "Cliente",
            ClienteEmail = cita.Cliente?.Email,
            ClienteTelefono = cita.Cliente?.Telefono,
            ServicioNombre = cita.Servicio?.Nombre ?? "Servicio",
            EstilistaId = cita.EstilistaId,
            EstilistaNombre = cita.Estilista?.NombreCompleto,
            MontoBase = cita.PrecioBase > 0 ? cita.PrecioBase : cita.PrecioTotal,
            Descuento = cita.Descuento,
            CuponCodigo = cita.CuponCodigo,
            MontoTotal = cita.PrecioTotal,
            MetodoPago = cita.MetodoPago?.ToString() ?? "Efectivo",
            NumeroOperacion = cita.NumeroOperacion,
            FechaEmision = DateTime.UtcNow,
            FechaServicio = cita.Inicio
        };

        try
        {
            _db.ComprobantesPago.Add(comprobante);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Comprobante {NumeroComprobante} generado exitosamente para cita {CitaId}", numeroComprobante, citaId);
            return comprobante;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al guardar comprobante para cita {CitaId}. Comprobando si fue generado en paralelo.", citaId);
            // Si ocurrió una colisión única por concurrencia, intentar retornar el existente
            return await _db.ComprobantesPago
                .Include(c => c.Cita)
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.CitaId == citaId);
        }
    }

    public async Task<ComprobantePago?> ObtenerPorCitaIdAsync(int citaId, int? clienteId = null)
    {
        var comprobante = await _db.ComprobantesPago
            .Include(c => c.Cita)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.CitaId == citaId);

        // Si no existe pero la cita ya fue pagada, generarlo automáticamente al vuelo
        if (comprobante == null)
        {
            comprobante = await GenerarComprobanteAsync(citaId);
        }

        if (comprobante == null) return null;

        if (clienteId.HasValue && comprobante.ClienteId != clienteId.Value)
        {
            return null; // No autorizado
        }

        return comprobante;
    }

    public async Task<ComprobantePago?> ObtenerPorIdAsync(int id, int? clienteId = null)
    {
        var comprobante = await _db.ComprobantesPago
            .Include(c => c.Cita)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comprobante == null) return null;

        if (clienteId.HasValue && comprobante.ClienteId != clienteId.Value)
        {
            return null; // No autorizado
        }

        return comprobante;
    }

    public async Task<ComprobanteViewModel?> ObtenerViewModelPorCitaIdAsync(int citaId, int? clienteId = null)
    {
        var comprobante = await ObtenerPorCitaIdAsync(citaId, clienteId);
        if (comprobante == null) return null;

        var cita = await _db.Citas
            .Include(c => c.Servicio)
                .ThenInclude(s => s!.Categoria)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == citaId);

        return MapearViewModel(comprobante, cita);
    }

    public async Task<ComprobanteViewModel?> ObtenerViewModelPorIdAsync(int id, int? clienteId = null)
    {
        var comprobante = await ObtenerPorIdAsync(id, clienteId);
        if (comprobante == null) return null;

        var cita = await _db.Citas
            .Include(c => c.Servicio)
                .ThenInclude(s => s!.Categoria)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == comprobante.CitaId);

        return MapearViewModel(comprobante, cita);
    }

    private static ComprobanteViewModel MapearViewModel(ComprobantePago c, Cita? cita)
    {
        var emisionLocal = TimeZoneInfo.ConvertTimeFromUtc(c.FechaEmision, ZonaSalon);
        var servicioInicioLocal = TimeZoneInfo.ConvertTimeFromUtc(c.FechaServicio, ZonaSalon);
        var servicioFinLocal = cita != null 
            ? TimeZoneInfo.ConvertTimeFromUtc(cita.Fin, ZonaSalon)
            : servicioInicioLocal.AddMinutes(cita?.Servicio?.DuracionMinutos ?? 45);

        return new ComprobanteViewModel
        {
            Id = c.Id,
            NumeroComprobante = c.NumeroComprobante,
            CitaId = c.CitaId,
            NumeroOperacion = c.NumeroOperacion,
            FechaEmision = emisionLocal,
            FechaCitaInicio = servicioInicioLocal,
            FechaCitaFin = servicioFinLocal,
            ClienteId = c.ClienteId,
            ClienteNombre = c.ClienteNombre,
            ClienteEmail = c.ClienteEmail ?? "",
            ClienteTelefono = c.ClienteTelefono ?? "",
            ServicioId = cita?.ServicioId ?? 0,
            ServicioNombre = c.ServicioNombre,
            CategoriaNombre = cita?.Servicio?.Categoria?.Nombre,
            DuracionMinutos = cita?.Servicio?.DuracionMinutos ?? (int)Math.Max(15, (servicioFinLocal - servicioInicioLocal).TotalMinutes),
            EstilistaNombre = c.EstilistaNombre ?? cita?.Estilista?.NombreCompleto ?? "Estilista D'Kaiza",
            PrecioBase = c.MontoBase,
            Descuento = c.Descuento,
            CuponCodigo = c.CuponCodigo,
            TotalPagado = c.MontoTotal,
            MetodoPago = c.MetodoPago,
            EstadoPago = "PAGADO"
        };
    }
}
