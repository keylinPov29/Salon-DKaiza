using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Cliente")]
[Route("Notificaciones")]
public class NotificacionesController : Controller
{
    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly ApplicationDbContext _db;

    public NotificacionesController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var recordatorios = await _db.NotificacionesCita
            .AsNoTracking()
            .Where(n => n.ClienteId == clienteId &&
                        n.ResueltaEn == null &&
                        n.Cita != null &&
                        n.Cita.Inicio > DateTime.UtcNow &&
                        n.Cita.Estado != EstadoCita.Cancelada &&
                        n.Cita.Estado != EstadoCita.Completada &&
                        n.Cita.Estado != EstadoCita.EnProceso)
            .OrderBy(n => n.Cita!.Inicio)
            .Select(n => new NotificacionClienteDto
            {
                NotificacionId = n.Id,
                CitaId = n.CitaId,
                Servicio = n.Cita!.Servicio!.Nombre,
                Estilista = n.Cita.Estilista!.Nombres + " " + n.Cita.Estilista.Apellidos,
                Inicio = n.Cita.Inicio,
                Fin = n.Cita.Fin,
                CreadaEn = n.CreadaEn,
                AsistenciaConfirmada = n.Cita.AsistenciaConfirmada,
                Precio = n.Cita.PrecioTotal,
                EstadoPago = n.Cita.Estado == EstadoCita.Pagada ? "Pagada" :
                    n.Cita.Estado == EstadoCita.PendientePago ? "Pendiente de pago" : "Pendiente"
            })
            .ToListAsync();

        return View(new NotificacionesClienteViewModel { Recordatorios = recordatorios });
    }

    [HttpGet("contador")]
    public async Task<IActionResult> Contador()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var ahora = DateTime.UtcNow;
        var count = await _db.NotificacionesCita
            .CountAsync(n => n.ClienteId == clienteId &&
                             n.ResueltaEn == null &&
                             n.Cita != null &&
                             n.Cita.Inicio > ahora &&
                             n.Cita.Estado != EstadoCita.Cancelada &&
                             n.Cita.Estado != EstadoCita.Completada &&
                             n.Cita.Estado != EstadoCita.EnProceso);

        return Json(new { count });
    }

    [HttpPost("{id:int}/confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarAsistencia(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var ahora = DateTime.UtcNow;
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var notificacion = await _db.NotificacionesCita
            .FirstOrDefaultAsync(n => n.Id == id && n.ClienteId == clienteId && n.ResueltaEn == null);

        if (notificacion == null)
        {
            TempData["Error"] = "El recordatorio ya no está disponible.";
            return RedirectToAction(nameof(Index));
        }

        var actualizadas = await _db.Citas
            .Where(c => c.Id == notificacion.CitaId &&
                        c.ClienteId == clienteId &&
                        c.Inicio > ahora &&
                        c.Estado != EstadoCita.Cancelada &&
                        c.Estado != EstadoCita.Completada &&
                        c.Estado != EstadoCita.EnProceso)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(c => c.AsistenciaConfirmada, true));

        if (actualizadas != 1)
        {
            await transaccion.RollbackAsync();
            TempData["Error"] = "La cita ya no se puede confirmar.";
            return RedirectToAction(nameof(Index));
        }

        notificacion.ResueltaEn = ahora;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        TempData["Success"] = "Asistencia confirmada. ¡Te esperamos!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarCita(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var ahora = DateTime.UtcNow;
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var notificacion = await _db.NotificacionesCita
            .FirstOrDefaultAsync(n => n.Id == id && n.ClienteId == clienteId && n.ResueltaEn == null);

        if (notificacion == null)
        {
            TempData["Error"] = "El recordatorio ya no está disponible.";
            return RedirectToAction(nameof(Index));
        }

        var actualizadas = await _db.Citas
            .Where(c => c.Id == notificacion.CitaId &&
                        c.ClienteId == clienteId &&
                        c.Inicio > ahora &&
                        c.Estado != EstadoCita.Cancelada &&
                        c.Estado != EstadoCita.Completada &&
                        c.Estado != EstadoCita.EnProceso)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(c => c.Estado, EstadoCita.Cancelada));

        if (actualizadas != 1)
        {
            await transaccion.RollbackAsync();
            TempData["Error"] = "La cita ya no se puede cancelar.";
            return RedirectToAction(nameof(Index));
        }

        notificacion.ResueltaEn = ahora;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        TempData["Success"] = "La cita fue cancelada y el horario quedó liberado.";
        return RedirectToAction(nameof(Index));
    }
}
