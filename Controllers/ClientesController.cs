using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

// HU-08 (parte del estilista): mis clientes, buscar clientes, ver su ficha y agregar notas
[Authorize(Roles = "Estilista")]
[Route("clientes")]
public class ClientesController : Controller
{
    private const int PorPaginaBusqueda = 10;
    private const int PorPaginaHistorial = 8;
    private const int MaxNotasVisibles = 20;
    private const int MaxLargoNota = 500;

    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
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

    // Evita que % y _ escritos por el usuario funcionen como comodines del LIKE
    private static string Escapar(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // Ids de las fichas de estilista de la persona que inició sesión.
    // Una misma persona puede tener una ficha por especialidad, así que se juntan por nombre.
    // Se usa el vínculo de la cuenta (EstilistaId) y, además, el nombre de la propia cuenta.
    private async Task<List<int>> IdsMisFichasAsync()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return new List<int>();

        var yo = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Nombre, u.Apellido, u.EstilistaId })
            .FirstOrDefaultAsync();
        if (yo == null) return new List<int>();

        var ids = new HashSet<int>();
        var nombres = new List<(string Nombres, string Apellidos)>();

        // 1) La ficha a la que está vinculada la cuenta
        if (yo.EstilistaId != null)
        {
            ids.Add(yo.EstilistaId.Value);

            var ficha = await _context.Estilistas.AsNoTracking()
                .Where(e => e.Id == yo.EstilistaId)
                .Select(e => new { e.Nombres, e.Apellidos })
                .FirstOrDefaultAsync();
            if (ficha != null)
                nombres.Add((ficha.Nombres.Trim().ToLower(), ficha.Apellidos.Trim().ToLower()));
        }

        // 2) El nombre de la propia cuenta
        nombres.Add((yo.Nombre.Trim().ToLower(), yo.Apellido.Trim().ToLower()));

        // 3) Todas las fichas con ese nombre (una por especialidad)
        foreach (var (n, a) in nombres.Distinct())
        {
            var mismas = await _context.Estilistas.AsNoTracking()
                .Where(e => e.Nombres.ToLower() == n && e.Apellidos.ToLower() == a)
                .Select(e => e.Id)
                .ToListAsync();
            ids.UnionWith(mismas);
        }

        return ids.ToList();
    }

    // ───────────── Pantalla 1: Mis clientes / buscar clientes ─────────────

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int pagina = 1)
    {
        var texto = (q ?? "").Trim();
        var vm = new ClientesBuscarViewModel
        {
            Q = texto,
            Buscado = texto.Length > 0,
            MisClientes = texto.Length == 0,
            PorPagina = PorPaginaBusqueda
        };

        // Todo lo que se muestra de las citas es solo con este estilista
        var misIds = await IdsMisFichasAsync();

        IQueryable<Usuario> query = _context.Usuarios.AsNoTracking().Where(u => u.Rol == RolUsuario.Cliente);
        IOrderedQueryable<Usuario> ordenada;

        if (vm.Buscado)
        {
            // Búsqueda entre todos los clientes: cada palabra debe coincidir
            // con nombre, apellido o teléfono
            foreach (var t in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var patron = $"%{Escapar(t)}%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.Nombre, patron) ||
                    EF.Functions.ILike(u.Apellido, patron) ||
                    EF.Functions.ILike(u.Telefono, patron));
            }

            // Primero los que más han venido con este estilista
            ordenada = query
                .OrderByDescending(u => _context.Citas.Count(c =>
                    c.ClienteId == u.Id && c.Estado == EstadoCita.Completada && misIds.Contains(c.EstilistaId)))
                .ThenBy(u => u.Nombre).ThenBy(u => u.Apellido);
        }
        else
        {
            // Lista por defecto: clientes que tienen (o tuvieron) citas con este estilista
            query = query.Where(u => _context.Citas.Any(c => c.ClienteId == u.Id && misIds.Contains(c.EstilistaId)));

            // Primero el de la cita más reciente
            ordenada = query
                .OrderByDescending(u => _context.Citas
                    .Where(c => c.ClienteId == u.Id && misIds.Contains(c.EstilistaId))
                    .Max(c => (DateTime?)c.Inicio))
                .ThenBy(u => u.Nombre).ThenBy(u => u.Apellido);
        }

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)PorPaginaBusqueda));
        vm.Pagina = Math.Clamp(pagina, 1, vm.TotalPaginas);

        var clientes = await ordenada
            .Skip((vm.Pagina - 1) * PorPaginaBusqueda)
            .Take(PorPaginaBusqueda)
            .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Telefono, u.Email })
            .ToListAsync();

        var ids = clientes.Select(c => c.Id).ToList();

        // Visitas y última visita (solo con este estilista)
        var stats = await _context.Citas.AsNoTracking()
            .Where(c => ids.Contains(c.ClienteId) && c.Estado == EstadoCita.Completada && misIds.Contains(c.EstilistaId))
            .GroupBy(c => c.ClienteId)
            .Select(g => new { ClienteId = g.Key, Visitas = g.Count(), Ultima = g.Max(x => x.Inicio) })
            .ToDictionaryAsync(x => x.ClienteId);

        // Próxima cita (no cancelada) con este estilista
        var ahora = DateTime.UtcNow;
        var proximas = await _context.Citas.AsNoTracking()
            .Where(c => ids.Contains(c.ClienteId) && c.Inicio >= ahora
                        && c.Estado != EstadoCita.Cancelada && misIds.Contains(c.EstilistaId))
            .GroupBy(c => c.ClienteId)
            .Select(g => new { ClienteId = g.Key, Proxima = g.Min(x => x.Inicio) })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Proxima);

        // Servicio más pedido de cada cliente (con este estilista)
        var servicios = await _context.Citas.AsNoTracking()
            .Where(c => ids.Contains(c.ClienteId) && c.Estado == EstadoCita.Completada && misIds.Contains(c.EstilistaId))
            .GroupBy(c => new { c.ClienteId, Servicio = c.Servicio!.Nombre })
            .Select(g => new { g.Key.ClienteId, g.Key.Servicio, Veces = g.Count() })
            .ToListAsync();

        var favorito = servicios
            .GroupBy(s => s.ClienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Veces).ThenBy(s => s.Servicio).First().Servicio);

        vm.Resultados = clientes.Select(c =>
        {
            stats.TryGetValue(c.Id, out var st);
            favorito.TryGetValue(c.Id, out var pref);
            DateTime? prox = proximas.TryGetValue(c.Id, out var p) ? Local(p) : null;

            return new ClienteFilaViewModel
            {
                Id = c.Id,
                Nombre = $"{c.Nombre} {c.Apellido}".Trim(),
                Telefono = c.Telefono,
                Email = c.Email,
                Visitas = st?.Visitas ?? 0,
                UltimaVisita = st == null ? null : Local(st.Ultima),
                ProximaCita = prox,
                Preferencia = pref
            };
        }).ToList();

        vm.MaxVisitas = vm.Resultados.Count == 0 ? 0 : vm.Resultados.Max(r => r.Visitas);
        return View(vm);
    }

    // ───────────── Pantalla 2: ficha del cliente ─────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id, int pagina = 1, string? q = null)
    {
        var cliente = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.Rol == RolUsuario.Cliente);
        if (cliente == null) return NotFound();

        // Solo las citas de este cliente con el estilista que inició sesión
        var misIds = await IdsMisFichasAsync();
        var citasConmigo = _context.Citas.AsNoTracking()
            .Where(c => c.ClienteId == id && misIds.Contains(c.EstilistaId));

        var completadas = citasConmigo.Where(c => c.Estado == EstadoCita.Completada);

        var visitas = await completadas.CountAsync();
        DateTime? ultima = visitas == 0 ? null : await completadas.MaxAsync(c => c.Inicio);

        // Preferencias: los servicios que más ha completado con este estilista
        var preferencias = await completadas
            .GroupBy(c => c.Servicio!.Nombre)
            .Select(g => new PreferenciaViewModel { Servicio = g.Key, Veces = g.Count() })
            .OrderByDescending(p => p.Veces).ThenBy(p => p.Servicio)
            .Take(3)
            .ToListAsync();

        // Notas (compartidas entre estilistas): más recientes primero
        var totalNotas = await _context.NotasCliente.CountAsync(n => n.ClienteId == id);
        var notasDb = await _context.NotasCliente.AsNoTracking()
            .Where(n => n.ClienteId == id)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(MaxNotasVisibles)
            .Select(n => new { n.Id, n.Texto, n.FechaCreacion, Autor = n.Autor!.Nombre + " " + n.Autor.Apellido })
            .ToListAsync();

        // Historial: más reciente primero, paginado
        var totalCitas = await citasConmigo.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalCitas / (double)PorPaginaHistorial));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var citas = await citasConmigo
            .OrderByDescending(c => c.Inicio)
            .Skip((pagina - 1) * PorPaginaHistorial)
            .Take(PorPaginaHistorial)
            .Select(c => new
            {
                Servicio = c.Servicio!.Nombre,
                Estilista = c.Estilista!.Nombres + " " + c.Estilista.Apellidos,
                c.Inicio,
                c.Estado
            })
            .ToListAsync();

        var ahora = DateTime.UtcNow;

        var vm = new ClienteDetalleViewModel
        {
            Id = cliente.Id,
            Nombre = $"{cliente.Nombre} {cliente.Apellido}".Trim(),
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            ClienteDesde = Local(cliente.FechaRegistro),
            Visitas = visitas,
            UltimaVisita = ultima == null ? null : Local(ultima.Value),
            TotalNotas = totalNotas,
            Preferencias = preferencias,
            Notas = notasDb.Select(n => new NotaClienteViewModel
            {
                Id = n.Id,
                Texto = n.Texto,
                Autor = n.Autor.Trim(),
                Fecha = Local(n.FechaCreacion)
            }).ToList(),
            Historial = citas.Select(c =>
            {
                var (texto, clase) = Estado(c.Estado, c.Inicio, ahora);
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
            TotalPaginas = totalPaginas,
            VolverQ = q
        };

        return View(vm);
    }

    // Asistió / Canceló / No asistió / Programada
    private static (string Texto, string Clase) Estado(EstadoCita estado, DateTime inicioUtc, DateTime ahoraUtc)
    {
        if (estado == EstadoCita.Completada) return ("Asistió", "ok");
        if (estado == EstadoCita.Cancelada) return ("Canceló", "rd");
        if (estado == EstadoCita.EnProceso) return ("En proceso", "pe");
        if (DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc) < ahoraUtc) return ("No asistió", "am");
        return ("Programada", "pe");
    }

    // ───────────── Agregar nota ─────────────

    [HttpPost("{id:int}/notas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarNota(int id, string? texto, string? q)
    {
        var existe = await _context.Usuarios.AnyAsync(u => u.Id == id && u.Rol == RolUsuario.Cliente);
        if (!existe) return NotFound();

        var limpio = (texto ?? "").Trim();

        if (limpio.Length == 0)
        {
            TempData["Error"] = "Escribe la nota antes de guardarla.";
            return RedirectToAction(nameof(Detalle), new { id, q });
        }

        if (limpio.Length > MaxLargoNota)
        {
            TempData["Error"] = $"La nota no puede superar los {MaxLargoNota} caracteres.";
            return RedirectToAction(nameof(Detalle), new { id, q });
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var autorId))
            return RedirectToAction("Login", "Account");

        _context.NotasCliente.Add(new NotaCliente
        {
            ClienteId = id,
            AutorId = autorId,
            Texto = limpio,
            FechaCreacion = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "Nota agregada.";
        return RedirectToAction(nameof(Detalle), new { id, q });
    }
}