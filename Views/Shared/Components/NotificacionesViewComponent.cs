using System.Security.Claims;
using DKaiza.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Views.Shared.Components;

public class NotificacionesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public NotificacionesViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!int.TryParse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return View(0);

        var count = await _db.NotificacionesCita
            .CountAsync(n => n.ClienteId == clienteId &&
                             n.ResueltaEn == null &&
                             n.Cita != null &&
                             n.Cita.Inicio > DateTime.UtcNow &&
                             n.Cita.Estado != DKaiza.Web.Dominio.EstadoCita.Cancelada &&
                             n.Cita.Estado != DKaiza.Web.Dominio.EstadoCita.Completada &&
                             n.Cita.Estado != DKaiza.Web.Dominio.EstadoCita.EnProceso);

        return View(count);
    }
}
