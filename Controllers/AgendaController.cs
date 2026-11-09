using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DKaiza.Web.Estilistas; 
using DKaiza.Web.Dominio;     
using DKaiza.Web.Models;      
using DKaiza.Data;            
using System.Security.Claims;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Estilista")]
public class AgendaController : Controller
{
    private readonly ApplicationDbContext _context; 

    public AgendaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? fecha)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));

        if (usuario == null || usuario.EstilistaId == null) return Unauthorized();

        // 1. Consulta base
        var query = _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
<<<<<<< Updated upstream
            .Include(c => c.Estilista)
            .Where(c => c.Estado != EstadoCita.Cancelada);

        bool esAdmin = usuario.Rol == RolUsuario.Administrador || usuario.Rol == RolUsuario.Recepcionista;

        // 2. Filtro por Rol
        if (usuario.Rol == RolUsuario.Estilista)
        {
            query = query.Where(c => c.EstilistaId == usuario.EstilistaId);
        }
        else if (esAdmin && estilistaIdFiltro.HasValue && estilistaIdFiltro > 0)
        {
            query = query.Where(c => c.EstilistaId == estilistaIdFiltro.Value);
        }
=======
            .Where(c => c.EstilistaId == usuario.EstilistaId && c.Estado != EstadoCita.Cancelada);
>>>>>>> Stashed changes

        // 3. Filtro por Fecha
        if (fecha.HasValue)
        {
            var inicioDia = fecha.Value.Date.ToUniversalTime();
            var finDia = inicioDia.AddDays(1);
            query = query.Where(c => c.Inicio >= inicioDia && c.Inicio < finDia);
        }
        else
        {
            query = query.Where(c => c.Inicio >= DateTime.UtcNow.Date);
        }

        var citas = await query.OrderBy(c => c.Inicio).ToListAsync();

<<<<<<< Updated upstream
        // 4. Cargar lista de estilistas para el combo
        var listaEstilistas = new List<Estilista>();
        if (esAdmin)
        {
            listaEstilistas = await _context.Estilistas
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombres)
                .ToListAsync();
        }

=======
>>>>>>> Stashed changes
        var viewModel = new AgendaEstilistaViewModel
        {
            FechaSeleccionada = fecha,
            EsAdmin = false,
            Citas = citas.Select(c => new CitaDto
            {
                Id = c.Id,
                ClienteNombre = $"{c.Cliente?.Nombre} {c.Cliente?.Apellido}",
                ClienteTelefono = c.Cliente?.Telefono ?? "N/A",
                ServicioNombre = c.Servicio?.Nombre ?? "N/A",
                EstilistaNombre = "Tú",
                Inicio = c.Inicio.ToLocalTime(),
                Fin = c.Fin.ToLocalTime(),
                PrecioTotal = c.PrecioTotal,
                Estado = c.Estado,
                Notas = c.Notas
            }).ToList()
        };

        return View(viewModel);
    }

    // ==========================================
    // VER DETALLE (Para el Modal)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> DetalleCita(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == int.Parse(userIdString));

        var cita = await _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cita == null || cita.EstilistaId != usuario.EstilistaId) return NotFound();

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

<<<<<<< Updated upstream
    // ==========================================
    // H.U. 12: INICIAR Y FINALIZAR ATENCIÓN
    // ==========================================
=======
>>>>>>> Stashed changes
    [HttpPost]
    public async Task<IActionResult> IniciarAtencion(int id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null) return Json(new { success = false, message = "Cita no encontrada." });

        if (cita.Estado != EstadoCita.Pendiente && cita.Estado != EstadoCita.Confirmada)
            return Json(new { success = false, message = "Solo se pueden iniciar citas Pendientes o Confirmadas." });

<<<<<<< Updated upstream
        cita.HoraInicioReal = DateTime.UtcNow;
        cita.Estado = EstadoCita.EnProceso; 
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Atención iniciada correctamente." });
=======
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
>>>>>>> Stashed changes
    }

    public async Task<IActionResult> EnProceso(int id)
    {
        var cita = await _context.Citas
            .Include(c => c.Cliente).Include(c => c.Servicio)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cita == null || cita.Estado != EstadoCita.EnProceso || cita.HoraInicioReal == null)
            return RedirectToAction(nameof(Index));

        ViewBag.HoraInicioStr = cita.HoraInicioReal.Value.ToLocalTime().ToString("HH:mm:ss");
<<<<<<< Updated upstream
        ViewBag.HoraInicioIso = cita.HoraInicioReal.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
=======
        ViewBag.HoraInicioMs = new DateTimeOffset(cita.HoraInicioReal.Value).ToUnixTimeMilliseconds();
>>>>>>> Stashed changes
        ViewBag.DuracionEstimada = (int)(cita.Fin - cita.Inicio).TotalMinutes;

        return View(cita);
    }

    [HttpPost]
    public async Task<IActionResult> FinalizarAtencion(int id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null) return Json(new { success = false, message = "Cita no encontrada." });

        if (cita.Estado != EstadoCita.EnProceso)
            return Json(new { success = false, message = "La cita debe estar En Proceso." });

<<<<<<< Updated upstream
        cita.HoraFinReal = DateTime.UtcNow;
        cita.Estado = EstadoCita.Completada;
        await _context.SaveChangesAsync();

        var tiempoTotal = cita.HoraFinReal.Value - cita.HoraInicioReal!.Value;

        return Json(new { 
            success = true, 
            message = "Atención finalizada correctamente.",
            resumen = new {
                horaInicio = cita.HoraInicioReal.Value.ToLocalTime().ToString("HH:mm:ss"),
                horaFin = cita.HoraFinReal.Value.ToLocalTime().ToString("HH:mm:ss"),
                tiempoTotal = tiempoTotal.ToString(@"hh\:mm\:ss")
            }
        });
=======
            if (cita.EstilistaId != usuario.EstilistaId) return Forbid();

            if (cita.Estado != EstadoCita.EnProceso) return BadRequest("La cita debe estar En Proceso.");

            cita.HoraFinReal = DateTime.UtcNow;
            cita.Estado = EstadoCita.Completada;
            
            await _context.SaveChangesAsync();

            TempData["Success"] = "¡Atención finalizada correctamente!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al finalizar: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction(nameof(Index));
        }
>>>>>>> Stashed changes
    }
}