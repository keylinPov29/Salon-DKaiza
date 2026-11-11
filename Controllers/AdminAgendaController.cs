using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Estilistas;
using DKaiza.Web.Models;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminAgendaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminAgendaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? fecha, int? estilistaIdFiltro, string? estadoFiltro)
    {
        var query = _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .AsQueryable();

        // Filtro por Fecha (Solo si el usuario selecciona una fecha)
        if (fecha.HasValue)
        {
            var inicioDia = fecha.Value.Date.ToUniversalTime();
            var finDia = inicioDia.AddDays(1);
            query = query.Where(c => c.Inicio >= inicioDia && c.Inicio < finDia);
        }
        // NOTA: Quitamos el 'else' que filtraba por el día de hoy, para que muestre todas.

        // Filtro por Estilista
        if (estilistaIdFiltro.HasValue && estilistaIdFiltro > 0)
        {
            query = query.Where(c => c.EstilistaId == estilistaIdFiltro.Value);
        }

        // Filtro por Estado (Las tarjetas)
        if (!string.IsNullOrEmpty(estadoFiltro))
        {
            if (estadoFiltro == "pendientes")
            {
                query = query.Where(c => c.Estado != EstadoCita.Completada && c.Estado != EstadoCita.Cancelada);
            }
            else if (estadoFiltro == "completadas")
            {
                query = query.Where(c => c.Estado == EstadoCita.Completada);
            }
            else if (estadoFiltro == "canceladas") 
            {
                query = query.Where(c => c.Estado == EstadoCita.Cancelada);
            }
        }

        // Traemos todas las citas que cumplan los filtros
        var citas = await query.OrderByDescending(c => c.Inicio).ToListAsync(); 
        // (Usé OrderByDescending para que las más recientes salgan primero)

        var listaEstilistas = await _context.Estilistas
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombres)
            .ToListAsync();

        var viewModel = new AgendaEstilistaViewModel
        {
            FechaSeleccionada = fecha,
            EstilistaIdFiltro = estilistaIdFiltro,
            EsAdmin = true,
            ListaEstilistas = listaEstilistas,
            Citas = citas.Select(c => new CitaDto
            {
                Id = c.Id,
                ClienteNombre = $"{c.Cliente?.Nombre} {c.Cliente?.Apellido}",
                ClienteTelefono = c.Cliente?.Telefono ?? "N/A",
                ServicioNombre = c.Servicio?.Nombre ?? "N/A",
                EstilistaNombre = c.Estilista?.NombreCompleto ?? "N/A",
                Inicio = c.Inicio.ToLocalTime(),
                Fin = c.Fin.ToLocalTime(),
                PrecioTotal = c.PrecioTotal,
                Estado = c.Estado,
                Notas = c.Notas
            }).ToList()
        };

        ViewBag.EstadoFiltroActual = estadoFiltro;
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> DetalleCita(int id)
    {
        var cita = await _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cita == null) return NotFound();

        var model = new DetalleCitaDto
        {
            Id = cita.Id,
            ClienteNombre = $"{cita.Cliente?.Nombre} {cita.Cliente?.Apellido}",
            ClienteTelefono = cita.Cliente?.Telefono ?? "",
            ServicioNombre = cita.Servicio?.Nombre ?? "",
            EstilistaNombre = cita.Estilista?.NombreCompleto ?? "",
            FechaStr = cita.Inicio.ToLocalTime().ToString("dddd, dd 'de' MMMM 'de' yyyy"),
            HorarioStr = $"{cita.Inicio.ToLocalTime():HH:mm} - {cita.Fin.ToLocalTime():HH:mm}",
            PrecioTotal = cita.PrecioTotal,
            Estado = cita.Estado,
            Notas = cita.Notas
        };

        return PartialView("~/Views/Agenda/_DetalleCitaPartial.cshtml", model);
    }
}