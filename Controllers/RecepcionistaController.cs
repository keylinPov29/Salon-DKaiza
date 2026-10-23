using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Recepcionista")]
[Route("Recepcionista")]
public class RecepcionistaController : Controller
{
    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly ApplicationDbContext _db;

    public RecepcionistaController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? desde, DateOnly? hasta, string? buscar)
    {
        var hoyLocal = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaSalon));
        var fechaDesde = desde ?? hoyLocal.AddDays(-30);
        var fechaHasta = hasta ?? hoyLocal;

        if (fechaDesde > fechaHasta)
        {
            ModelState.AddModelError("", "La fecha inicial no puede ser posterior a la fecha final.");
            fechaDesde = fechaHasta;
        }

        var pendientesQuery = _db.Citas
            .AsNoTracking()
            .Where(c => c.Estado == EstadoCita.PendientePago &&
                        c.MetodoPago == MetodoPagoCita.Efectivo);

        var pagosPendientes = await pendientesQuery
            .CountAsync();
        var totalPendiente = await pendientesQuery
            .Select(c => (decimal?)c.PrecioTotal)
            .SumAsync() ?? 0m;

        var inicioHoyUtc = TimeZoneInfo.ConvertTimeToUtc(hoyLocal.ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var inicioMananaUtc = TimeZoneInfo.ConvertTimeToUtc(hoyLocal.AddDays(1).ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var totalCobradoHoy = await _db.Citas
            .Where(c => c.Estado == EstadoCita.Pagada &&
                        c.FechaPago >= inicioHoyUtc &&
                        c.FechaPago < inicioMananaUtc)
            .Select(c => (decimal?)c.PrecioTotal)
            .SumAsync() ?? 0m;

        var inicioFiltroUtc = TimeZoneInfo.ConvertTimeToUtc(fechaDesde.ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var finFiltroUtc = TimeZoneInfo.ConvertTimeToUtc(fechaHasta.AddDays(1).ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var historialQuery = _db.Citas
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .Where(c => c.Estado == EstadoCita.Pagada &&
                        c.FechaPago >= inicioFiltroUtc &&
                        c.FechaPago < finFiltroUtc);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            historialQuery = historialQuery.Where(c =>
                (c.Cliente != null && (c.Cliente.Nombre + " " + c.Cliente.Apellido).Contains(termino)) ||
                (c.Cliente != null && c.Cliente.Email.Contains(termino)) ||
                (c.NumeroOperacion != null && c.NumeroOperacion.Contains(termino)));
        }

        var pendientes = await pendientesQuery
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .OrderBy(c => c.Inicio)
            .Select(c => new PagoRecepcionDto
            {
                CitaId = c.Id,
                Cliente = c.Cliente == null ? "" : c.Cliente.Nombre + " " + c.Cliente.Apellido,
                Email = c.Cliente == null ? "" : c.Cliente.Email,
                Telefono = c.Cliente == null ? "" : c.Cliente.Telefono,
                Servicio = c.Servicio == null ? "" : c.Servicio.Nombre,
                Estilista = c.Estilista == null ? "" : c.Estilista.Nombres + " " + c.Estilista.Apellidos,
                Inicio = c.Inicio,
                Monto = c.PrecioTotal,
                MetodoPago = c.MetodoPago,
                Estado = c.Estado,
                CuponCodigo = c.CuponCodigo
            })
            .ToListAsync();

        var historial = await historialQuery
            .OrderByDescending(c => c.FechaPago)
            .Select(c => new PagoRecepcionDto
            {
                CitaId = c.Id,
                Cliente = c.Cliente == null ? "" : c.Cliente.Nombre + " " + c.Cliente.Apellido,
                Email = c.Cliente == null ? "" : c.Cliente.Email,
                Telefono = c.Cliente == null ? "" : c.Cliente.Telefono,
                Servicio = c.Servicio == null ? "" : c.Servicio.Nombre,
                Estilista = c.Estilista == null ? "" : c.Estilista.Nombres + " " + c.Estilista.Apellidos,
                Inicio = c.Inicio,
                Monto = c.PrecioTotal,
                MetodoPago = c.MetodoPago,
                Estado = c.Estado,
                FechaPago = c.FechaPago,
                NumeroOperacion = c.NumeroOperacion,
                CuponCodigo = c.CuponCodigo
            })
            .ToListAsync();

        return View(new RecepcionistaViewModel
        {
            Desde = fechaDesde,
            Hasta = fechaHasta,
            Buscar = buscar,
            PagosPendientesCount = pagosPendientes,
            TotalPendiente = totalPendiente,
            TotalCobradoHoy = totalCobradoHoy,
            Pendientes = pendientes,
            Historial = historial
        });
    }

    [HttpPost("confirmar-efectivo/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPagoEfectivo(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var recepcionistaId))
            return Forbid();

        var fechaPago = DateTime.UtcNow;
        var operacion = $"EF-{id}-{fechaPago:yyyyMMddHHmmss}";
        var actualizadas = await _db.Citas
            .Where(c => c.Id == id &&
                        c.Estado == EstadoCita.PendientePago &&
                        c.MetodoPago == MetodoPagoCita.Efectivo &&
                        c.FechaPago == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Estado, EstadoCita.Pagada)
                .SetProperty(c => c.FechaPago, fechaPago)
                .SetProperty(c => c.NumeroOperacion, operacion));

        if (actualizadas != 1)
        {
            TempData["Error"] = "El pago ya fue registrado o la reserva no está pendiente de pago en efectivo.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Pago en efectivo confirmado. Operación {operacion}.";
        return RedirectToAction(nameof(Index));
    }
}
