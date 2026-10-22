using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DKaiza.Web.Estilistas; 
using DKaiza.Web.Dominio;     
using DKaiza.Web.Models;      
using DKaiza.Data;            
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DKaiza.Web.Controllers;

public class AgendaController : Controller
{
    private readonly ApplicationDbContext _context; 

    public AgendaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // H.U. 11: VISUALIZAR AGENDA Y DETALLE
    // ==========================================
    public async Task<IActionResult> Index(DateTime? fecha, int? estilistaIdFiltro)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account"); 
        
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));
        if (usuario == null) return Unauthorized();

        var query = _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .Where(c => c.Estado != EstadoCita.Cancelada);

        bool esAdmin = usuario.Rol == RolUsuario.Administrador || usuario.Rol == RolUsuario.Recepcionista;

        if (usuario.Rol == RolUsuario.Estilista)
        {
            query = query.Where(c => c.EstilistaId == usuario.EstilistaId);
        }
        else if (esAdmin && estilistaIdFiltro.HasValue && estilistaIdFiltro > 0)
        {
            query = query.Where(c => c.EstilistaId == estilistaIdFiltro.Value);
        }

        if (fecha.HasValue)
        {
            var inicioDia = fecha.Value.Date.ToUniversalTime();
            var finDia = inicioDia.AddDays(1);
            query = query.Where(c => c.Inicio >= inicioDia && c.Inicio < finDia);
        }
        else
        {
            var hoyUtc = DateTime.UtcNow.Date;
            query = query.Where(c => c.Inicio >= hoyUtc);
        }

        var citas = await query.OrderBy(c => c.Inicio).ToListAsync();

        var listaEstilistas = new List<Estilista>();
        if (esAdmin)
        {
            listaEstilistas = await _context.Estilistas
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombres)
                .ToListAsync();
        }

        var viewModel = new AgendaEstilistaViewModel
        {
            FechaSeleccionada = fecha,
            EstilistaIdFiltro = estilistaIdFiltro,
            EsAdmin = esAdmin,
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

        return PartialView("_DetalleCitaPartial", model);
    }

    // ==========================================
    // H.U. 12: INICIAR Y FINALIZAR ATENCIÓN
    // ==========================================
    
    [HttpPost]
    [Authorize(Roles = "Estilista")] 
    public async Task<IActionResult> IniciarAtencion(int id)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));

            if (usuario == null || usuario.EstilistaId == null) return Unauthorized();

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            if (cita.EstilistaId != usuario.EstilistaId) return Forbid();

            if (cita.Estado != EstadoCita.Pendiente && cita.Estado != EstadoCita.Confirmada)
                return BadRequest("Solo se pueden iniciar citas Pendientes o Confirmadas.");

            cita.HoraInicioReal = DateTime.UtcNow;
            cita.Estado = EstadoCita.EnProceso; 
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(EnProceso), new { id = cita.Id });
        }
        catch (Exception ex)
        {
            return BadRequest("Error al iniciar: " + ex.Message);
        }
    }

    [Authorize(Roles = "Estilista")] 
    public async Task<IActionResult> EnProceso(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));

        var cita = await _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cita == null || cita.EstilistaId != usuario.EstilistaId || cita.Estado != EstadoCita.EnProceso || cita.HoraInicioReal == null)
            return RedirectToAction(nameof(Index));

        ViewBag.HoraInicioStr = cita.HoraInicioReal.Value.ToLocalTime().ToString("HH:mm:ss");
        // Enviamos la fecha de inicio en milisegundos para que JS no falle
        ViewBag.HoraInicioMs = new DateTimeOffset(cita.HoraInicioReal.Value).ToUnixTimeMilliseconds();
        ViewBag.DuracionEstimada = (int)(cita.Fin - cita.Inicio).TotalMinutes;

        return View(cita);
    }

    [HttpPost]
    [Authorize(Roles = "Estilista")] 
    public async Task<IActionResult> FinalizarAtencion(int id)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            if (cita.EstilistaId != usuario.EstilistaId)
                return Forbid();

            if (cita.Estado != EstadoCita.EnProceso)
                return BadRequest("La cita debe estar En Proceso.");

            // Guardamos la hora de fin y cambiamos el estado
            cita.HoraFinReal = DateTime.UtcNow;
            cita.Estado = EstadoCita.Completada;
            
            await _context.SaveChangesAsync();

            // Guardamos el mensaje en TempData para mostrarlo en la agenda
            TempData["Success"] = "¡Atención finalizada correctamente!";
            
            // Redirigimos a la agenda
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al finalizar: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }
}