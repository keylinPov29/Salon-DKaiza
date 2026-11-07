using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

// HU-08 (parte del administrador): gestión del historial de clientes
[Authorize(Roles = "Administrador")]
[Route("admin/clientes")]
public class AdminClientesController : Controller
{
    private const int PorPaginaClientes = 10;
    private const int PorPaginaRegistros = 8;

    // Frecuencia: asistencias (citas completadas) en los últimos 6 meses
    private const int MesesFrecuencia = 6;
    private const int AsistenciasAlta = 5;   // 5 o más = alta (y VIP automático)
    private const int AsistenciasMedia = 2;  // 2 a 4 = media · 0 a 1 = baja

    private readonly ApplicationDbContext _context;

    public AdminClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ───────────── Zona horaria del salón (las citas se guardan en UTC) ─────────────

    private static readonly TimeZoneInfo Zona = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
    {
        foreach (var id in new[] { "SA Pacific Standard Time", "America/Lima" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* probar el siguiente id */ }
        }
        return TimeZoneInfo.Utc;
    }

    private static DateTime Local(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    // Medianoche local -> UTC
    private static DateTime ALaUtc(DateTime fechaLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified), Zona);

    // ───────────── Reglas de frecuencia y VIP ─────────────

    private static string Frecuencia(int asistencias) =>
        asistencias >= AsistenciasAlta ? "alta"
        : asistencias >= AsistenciasMedia ? "media"
        : "baja";

    // VIP manual (si la administradora lo decidió) o automático (frecuencia alta)
    private static bool EsVip(bool? vipManual, int asistenciasRecientes) =>
        vipManual ?? (asistenciasRecientes >= AsistenciasAlta);

    // ───────────── Pantalla 1: Gestión de historial de clientes ─────────────

    [HttpGet("")]
    public async Task<IActionResult> Index(int pagina = 1)
    {
        var ahora = DateTime.UtcNow;
        var corte = ahora.AddMonths(-MesesFrecuencia);

        var clientes = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Rol == RolUsuario.Cliente)
            .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Telefono, u.VipManual })
            .ToListAsync();

        // Asistencias de toda la historia de cada cliente
        var totales = await _context.Citas.AsNoTracking()
            .Where(c => c.Estado == EstadoCita.Completada)
            .GroupBy(c => c.ClienteId)
            .Select(g => new { ClienteId = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.ClienteId, x => x.N);

        // Asistencias de los últimos 6 meses (para la frecuencia)
        var recientes = await _context.Citas.AsNoTracking()
            .Where(c => c.Estado == EstadoCita.Completada && c.Inicio >= corte && c.Inicio <= ahora)
            .GroupBy(c => c.ClienteId)
            .Select(g => new { ClienteId = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.ClienteId, x => x.N);

        var filas = clientes.Select(c =>
        {
            totales.TryGetValue(c.Id, out var total);
            recientes.TryGetValue(c.Id, out var rec);
            return new AdminClienteFilaViewModel
            {
                Id = c.Id,
                Nombre = $"{c.Nombre} {c.Apellido}".Trim(),
                Telefono = c.Telefono,
                Asistencias = total,
                Frecuencia = Frecuencia(rec),
                EsVip = EsVip(c.VipManual, rec)
            };
        })
        .OrderByDescending(f => f.Asistencias).ThenBy(f => f.Nombre)
        .ToList();

        var vm = new AdminClientesViewModel
        {
            TotalClientes = filas.Count,
            ClientesVip = filas.Count(f => f.EsVip),
            TotalAsistencias = filas.Sum(f => f.Asistencias),
            PorPagina = PorPaginaClientes
        };
        vm.PromedioPorCliente = vm.TotalClientes == 0
            ? 0
            : Math.Round((double)vm.TotalAsistencias / vm.TotalClientes, 1);

        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(filas.Count / (double)PorPaginaClientes));
        vm.Pagina = Math.Clamp(pagina, 1, vm.TotalPaginas);
        vm.MaxAsistencias = filas.Count == 0 ? 0 : filas.Max(f => f.Asistencias);
        vm.Clientes = filas.Skip((vm.Pagina - 1) * PorPaginaClientes).Take(PorPaginaClientes).ToList();

        return View(vm);
    }

    // ───────────── Pantalla 2: Historial del cliente ─────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Historial(int id, DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        var cliente = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.Rol == RolUsuario.Cliente);
        if (cliente == null) return NotFound();

        var ahora = DateTime.UtcNow;
        var corte = ahora.AddMonths(-MesesFrecuencia);

        // Frecuencia y VIP (no dependen del filtro de fechas)
        var recientes = await _context.Citas.AsNoTracking()
            .CountAsync(c => c.ClienteId == id && c.Estado == EstadoCita.Completada
                             && c.Inicio >= corte && c.Inicio <= ahora);

        // Si las fechas vienen al revés, se intercambian
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            (desde, hasta) = (hasta, desde);

        // Un "registro" es una cita completada, cancelada o ya pasada sin asistir.
        // Las citas futuras y la que está en proceso todavía no cuentan.
        var registros = _context.Citas.AsNoTracking()
            .Where(c => c.ClienteId == id)
            .Where(c => c.Estado == EstadoCita.Completada
                     || c.Estado == EstadoCita.Cancelada
                     || (c.Estado != EstadoCita.EnProceso && c.Inicio < ahora));

        if (desde.HasValue)
        {
            var desdeUtc = ALaUtc(desde.Value.Date);
            registros = registros.Where(c => c.Inicio >= desdeUtc);
        }
        if (hasta.HasValue)
        {
            var hastaUtc = ALaUtc(hasta.Value.Date.AddDays(1));
            registros = registros.Where(c => c.Inicio < hastaUtc);
        }

        var asistencias = await registros.CountAsync(c => c.Estado == EstadoCita.Completada);
        var cancelaciones = await registros.CountAsync(c => c.Estado == EstadoCita.Cancelada);
        var total = await registros.CountAsync();

        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPaginaRegistros));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var citas = await registros
            .OrderByDescending(c => c.Inicio)
            .Skip((pagina - 1) * PorPaginaRegistros)
            .Take(PorPaginaRegistros)
            .Select(c => new
            {
                Servicio = c.Servicio!.Nombre,
                Estilista = c.Estilista!.Nombres + " " + c.Estilista.Apellidos,
                c.Inicio,
                c.Estado
            })
            .ToListAsync();

        var vm = new AdminClienteHistorialViewModel
        {
            Id = cliente.Id,
            Nombre = $"{cliente.Nombre} {cliente.Apellido}".Trim(),
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            EsVip = EsVip(cliente.VipManual, recientes),
            Frecuencia = Frecuencia(recientes),
            AsistenciasUltimos6Meses = recientes,
            TotalRegistros = total,
            Asistencias = asistencias,
            Cancelaciones = cancelaciones,
            Ausencias = total - asistencias - cancelaciones,
            Desde = desde,
            Hasta = hasta,
            Registros = citas.Select(c =>
            {
                var (texto, clase) = Estado(c.Estado);
                return new HistorialClienteItemViewModel
                {
                    Servicio = c.Servicio,
                    Estilista = c.Estilista.Trim(),
                    Inicio = Local(c.Inicio),
                    EstadoTexto = texto,
                    EstadoClase = clase
                };
            }).ToList(),
            Pagina = pagina,
            TotalPaginas = totalPaginas
        };

        return View(vm);
    }

    // Todo registro es Asistió, Canceló o No asistió
    private static (string Texto, string Clase) Estado(EstadoCita estado)
    {
        if (estado == EstadoCita.Completada) return ("Asistió", "ok");
        if (estado == EstadoCita.Cancelada) return ("Canceló", "rd");
        return ("No asistió", "am");
    }

    // ───────────── Marcar / quitar VIP ─────────────

    [HttpPost("{id:int}/vip")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarVip(int id, string? desde, string? hasta)
    {
        var cliente = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id && u.Rol == RolUsuario.Cliente);
        if (cliente == null) return NotFound();

        var ahora = DateTime.UtcNow;
        var corte = ahora.AddMonths(-MesesFrecuencia);

        var recientes = await _context.Citas.AsNoTracking()
            .CountAsync(c => c.ClienteId == id && c.Estado == EstadoCita.Completada
                             && c.Inicio >= corte && c.Inicio <= ahora);

        var automatico = recientes >= AsistenciasAlta;
        var actual = cliente.VipManual ?? automatico;
        var nuevo = !actual;

        // Si lo que se pide coincide con lo automático, se vuelve al modo automático (null)
        cliente.VipManual = nuevo == automatico ? null : nuevo;
        await _context.SaveChangesAsync();

        var nombre = $"{cliente.Nombre} {cliente.Apellido}".Trim();
        TempData["Success"] = nuevo
            ? $"{nombre} ahora es cliente VIP."
            : $"Se quitó el VIP a {nombre}.";

        return RedirectToAction(nameof(Historial), new { id, desde, hasta });
    }
}