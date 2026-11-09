using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

/// <summary>
/// HU-14 · Calificar servicios y estilistas (vista del cliente).
/// Solo se pueden calificar las citas Completadas del propio cliente, una vez por cita.
/// </summary>
[Authorize(Roles = "Cliente")]
public class CalificacionesController : Controller
{
    private const int ComentariosPorPagina = 10;
    private const int MaxComentario = 250;

    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly ApplicationDbContext _db;

    public CalificacionesController(ApplicationDbContext db) => _db = db;

    private int? ClienteActualId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaSalon);

    // "Camila" + "Rojas Pérez"  ->  "Camila R."
    private static string Abreviar(string nombre, string apellido)
    {
        var n = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var a = (apellido ?? "").Trim();
        return a.Length == 0 ? n : $"{n} {char.ToUpperInvariant(a[0])}.";
    }

    // Una misma persona puede estar registrada varias veces (una por especialidad).
    // Se agrupa por nombre completo (sin tildes ni mayúsculas) para mostrarla una sola vez.
    private static string Clave(string nombres, string apellidos)
    {
        var s = $"{nombres} {apellidos}".Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return string.Join(' ', sb.ToString().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record FilaEstilista(int Id, string Nombres, string Apellidos, string Especialidad,
                                        TimeOnly Inicio, TimeOnly Fin, bool TieneFoto);

    private async Task<List<FilaEstilista>> CargarActivasAsync() =>
        await _db.Estilistas.AsNoTracking()
            .Where(e => e.Activo)
            .Select(e => new FilaEstilista(e.Id, e.Nombres, e.Apellidos, e.Especialidad,
                                           e.InicioJornada, e.FinJornada, e.Foto != null))
            .ToListAsync();

    // ───────── Historial ─────────

    [HttpGet("/cuenta/historial")]
    public async Task<IActionResult> Historial()
    {
        if (ClienteActualId() is not int clienteId) return Forbid();

        // Todo lo que ya ocurrió: citas a las que asistió (Completada) y las canceladas
        var citas = await _db.Citas.AsNoTracking()
            .Where(c => c.ClienteId == clienteId
                     && (c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.Cancelada))
            .OrderByDescending(c => c.Inicio)
            .Select(c => new
            {
                c.Id,
                c.Inicio,
                c.Fin,
                c.PrecioTotal,
                c.Estado,
                Servicio = c.Servicio!.Nombre,
                c.EstilistaId,
                Nombres = c.Estilista!.Nombres,
                Apellidos = c.Estilista!.Apellidos,
                TieneFoto = c.Estilista!.Foto != null
            })
            .ToListAsync();

        var calificadas = await _db.Calificaciones.AsNoTracking()
            .Where(k => k.ClienteId == clienteId)
            .Select(k => new { k.CitaId, k.Estrellas, k.Comentario })
            .ToDictionaryAsync(k => k.CitaId);

        var vm = new HistorialViewModel
        {
            Items = citas.Select(c =>
            {
                calificadas.TryGetValue(c.Id, out var k);
                var clase = c.Estado == EstadoCita.Cancelada ? "can" : (k != null ? "rat" : "por");

                return new HistorialItemViewModel
                {
                    Id = c.Id,
                    Inicio = Local(c.Inicio),
                    Fin = Local(c.Fin),
                    Servicio = c.Servicio,
                    EstilistaId = c.EstilistaId,
                    Estilista = $"{c.Nombres} {c.Apellidos}".Trim(),
                    EstilistaTieneFoto = c.TieneFoto,
                    Precio = c.PrecioTotal,
                    Clase = clase,
                    Estrellas = k?.Estrellas,
                    Comentario = k?.Comentario
                };
            }).ToList()
        };

        return View(vm);
    }

    // ───────── Calificar ─────────

    [HttpGet("/cuenta/historial/{id:int}/calificar")]
    public async Task<IActionResult> Calificar(int id)
    {
        if (ClienteActualId() is not int clienteId) return Forbid();

        var vm = await CargarCalificarAsync(id, clienteId);
        if (vm == null) return NotFound();

        if (!vm.Asistio)
        {
            TempData["Error"] = "Solo puedes calificar los servicios a los que asististe.";
            return Redirect("/cuenta/historial");
        }

        // Después de guardar se vuelve aquí para mostrar el mensaje de agradecimiento
        if (vm.YaCalificada && TempData["Gracias"] != null) vm.Gracias = true;

        return View(vm);
    }

    [HttpPost("/cuenta/historial/{id:int}/calificar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calificar(int id, int estrellas, string? comentario)
    {
        if (ClienteActualId() is not int clienteId) return Forbid();

        var vm = await CargarCalificarAsync(id, clienteId);
        if (vm == null) return NotFound();

        if (!vm.Asistio)
        {
            TempData["Error"] = "Solo puedes calificar los servicios a los que asististe.";
            return Redirect("/cuenta/historial");
        }

        if (vm.YaCalificada)
        {
            TempData["Error"] = "Ya calificaste este servicio.";
            return Redirect("/cuenta/historial");
        }

        comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();

        if (estrellas < 1 || estrellas > 5)
            ModelState.AddModelError(string.Empty, "Selecciona de 1 a 5 estrellas.");
        if (comentario != null && comentario.Length > MaxComentario)
            ModelState.AddModelError(string.Empty, $"El comentario no puede superar los {MaxComentario} caracteres.");

        if (!ModelState.IsValid)
        {
            vm.Estrellas = estrellas;
            vm.Comentario = comentario;
            return View(vm);
        }

        _db.Calificaciones.Add(new Calificacion
        {
            CitaId = id,
            EstilistaId = vm.EstilistaId,
            ClienteId = clienteId,
            Estrellas = estrellas,
            Comentario = comentario,
            FechaCreacion = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Si dos pestañas guardan a la vez, el índice único evita la segunda
            _db.ChangeTracker.Clear();
            TempData["Error"] = "Ya calificaste este servicio.";
            return Redirect("/cuenta/historial");
        }

        // El promedio de la estilista se calcula al consultarla, así que ya incluye esta calificación.
        TempData["Gracias"] = true;
        return Redirect($"/cuenta/historial/{id}/calificar");
    }

    private async Task<CalificarViewModel?> CargarCalificarAsync(int citaId, int clienteId)
    {
        // La cita debe ser del propio cliente; si no, se responde como si no existiera
        var c = await _db.Citas.AsNoTracking()
            .Where(x => x.Id == citaId && x.ClienteId == clienteId)
            .Select(x => new
            {
                x.Id,
                x.Inicio,
                x.Fin,
                x.PrecioTotal,
                x.Estado,
                Servicio = x.Servicio!.Nombre,
                x.EstilistaId,
                Nombres = x.Estilista!.Nombres,
                Apellidos = x.Estilista!.Apellidos,
                Especialidad = x.Estilista!.Especialidad,
                TieneFoto = x.Estilista!.Foto != null
            })
            .FirstOrDefaultAsync();

        if (c == null) return null;

        var guardada = await _db.Calificaciones.AsNoTracking()
            .Where(k => k.CitaId == citaId)
            .Select(k => new { k.Estrellas, k.Comentario })
            .FirstOrDefaultAsync();

        var u = await _db.Usuarios.AsNoTracking()
            .Where(x => x.Id == clienteId)
            .Select(x => new { x.Nombre, x.Apellido })
            .FirstOrDefaultAsync();

        return new CalificarViewModel
        {
            CitaId = c.Id,
            Servicio = c.Servicio,
            Inicio = Local(c.Inicio),
            Fin = Local(c.Fin),
            Precio = c.PrecioTotal,
            EstilistaId = c.EstilistaId,
            Estilista = $"{c.Nombres} {c.Apellidos}".Trim(),
            Especialidad = c.Especialidad,
            EstilistaTieneFoto = c.TieneFoto,
            ClienteAbreviado = u == null ? "" : Abreviar(u.Nombre, u.Apellido),
            Asistio = c.Estado == EstadoCita.Completada,
            YaCalificada = guardada != null,
            Estrellas = guardada?.Estrellas ?? 0,
            Comentario = guardada?.Comentario
        };
    }

    // ───────── Estilistas (listado) ─────────

    [HttpGet("/estilistas")]
    public async Task<IActionResult> Estilistas()
    {
        var activas = await CargarActivasAsync();

        var stats = await _db.Calificaciones.AsNoTracking()
            .GroupBy(k => k.EstilistaId)
            .Select(g => new { Id = g.Key, Suma = g.Sum(k => k.Estrellas), Total = g.Count() })
            .ToDictionaryAsync(x => x.Id);

        var tarjetas = new List<EstilistaTarjetaViewModel>();

        foreach (var grupo in activas.GroupBy(f => Clave(f.Nombres, f.Apellidos)))
        {
            var filas = grupo.OrderBy(f => f.Id).ToList();
            var principal = filas.FirstOrDefault(f => f.TieneFoto) ?? filas[0];
            var ids = filas.Select(f => f.Id).ToList();

            // Las calificaciones de todos sus registros se suman en un solo promedio
            var suma = 0;
            var total = 0;
            foreach (var f in filas)
                if (stats.TryGetValue(f.Id, out var st)) { suma += st.Suma; total += st.Total; }

            var ultimo = await _db.Calificaciones.AsNoTracking()
                .Where(k => ids.Contains(k.EstilistaId) && k.Comentario != null && k.Comentario != "")
                .OrderByDescending(k => k.FechaCreacion)
                .Select(k => new
                {
                    k.Comentario,
                    Nombre = k.Cliente!.Nombre,
                    Apellido = k.Cliente!.Apellido,
                    Servicio = k.Cita!.Servicio!.Nombre
                })
                .FirstOrDefaultAsync();

            var especialidades = filas
                .Select(f => f.Especialidad)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            tarjetas.Add(new EstilistaTarjetaViewModel
            {
                Id = principal.Id,
                Nombre = $"{principal.Nombres} {principal.Apellidos}".Trim(),
                Especialidades = especialidades,
                Especialidad = string.Join(" · ", especialidades),
                TieneFoto = filas.Any(f => f.TieneFoto),
                Promedio = total == 0 ? null : suma / (double)total,
                Total = total,
                UltimoComentario = ultimo?.Comentario,
                UltimoCliente = ultimo == null ? null : Abreviar(ultimo.Nombre, ultimo.Apellido),
                UltimoServicio = ultimo?.Servicio
            });
        }

        // Mejor calificadas primero; las nuevas (sin calificaciones) al final
        var vm = new EstilistasViewModel
        {
            Estilistas = tarjetas
                .OrderByDescending(t => t.Promedio ?? -1)
                .ThenByDescending(t => t.Total)
                .ThenBy(t => t.Nombre)
                .ToList()
        };

        return View(vm);
    }

    // ───────── Perfil de una estilista (opiniones) ─────────

    [HttpGet("/estilistas/{id:int}")]
    public async Task<IActionResult> PerfilEstilista(int id, int? estrellas, int pagina = 1)
    {
        var activas = await CargarActivasAsync();

        var e = activas.FirstOrDefault(x => x.Id == id);
        if (e == null) return NotFound();

        // Todos los registros de la misma persona (uno por especialidad)
        var clave = Clave(e.Nombres, e.Apellidos);
        var filas = activas.Where(x => Clave(x.Nombres, x.Apellidos) == clave).OrderBy(x => x.Id).ToList();
        var ids = filas.Select(x => x.Id).ToList();
        var principal = filas.FirstOrDefault(x => x.TieneFoto) ?? filas[0];

        // Promedio y distribución usan TODAS las calificaciones (con o sin comentario)
        var porEstrellas = await _db.Calificaciones.AsNoTracking()
            .Where(k => ids.Contains(k.EstilistaId))
            .GroupBy(k => k.Estrellas)
            .Select(g => new { Estrellas = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var distribucion = new int[6];
        foreach (var d in porEstrellas)
            if (d.Estrellas >= 1 && d.Estrellas <= 5) distribucion[d.Estrellas] = d.Cantidad;

        var total = distribucion.Sum();
        double? promedio = total == 0
            ? null
            : Enumerable.Range(1, 5).Sum(n => n * distribucion[n]) / (double)total;

        // La lista muestra solo las calificaciones que traen comentario
        var filtro = estrellas is 5 or 4 or 3 ? estrellas : null;
        var consulta = _db.Calificaciones.AsNoTracking()
            .Where(k => ids.Contains(k.EstilistaId) && k.Comentario != null && k.Comentario != "");

        if (filtro == 3) consulta = consulta.Where(k => k.Estrellas <= 3);
        else if (filtro != null) consulta = consulta.Where(k => k.Estrellas == filtro);

        var totalComentarios = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalComentarios / (double)ComentariosPorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var lista = await consulta
            .OrderByDescending(k => k.FechaCreacion)
            .Skip((pagina - 1) * ComentariosPorPagina)
            .Take(ComentariosPorPagina)
            .Select(k => new
            {
                k.Id,
                k.Estrellas,
                k.Comentario,
                k.FechaCreacion,
                Nombre = k.Cliente!.Nombre,
                Apellido = k.Cliente!.Apellido,
                Servicio = k.Cita!.Servicio!.Nombre
            })
            .ToListAsync();

        var especialidades = filas
            .Select(f => f.Especialidad)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vm = new PerfilEstilistaViewModel
        {
            Id = principal.Id,
            Nombre = $"{principal.Nombres} {principal.Apellidos}".Trim(),
            Especialidades = especialidades,
            Especialidad = string.Join(" · ", especialidades),
            TieneFoto = principal.TieneFoto,
            InicioJornada = filas.Min(x => x.Inicio),
            FinJornada = filas.Max(x => x.Fin),
            Promedio = promedio,
            TotalCalificaciones = total,
            Distribucion = distribucion,
            Hoy = Local(DateTime.UtcNow).Date,
            Filtro = filtro,
            Pagina = pagina,
            TotalPaginas = totalPaginas,
            TotalComentarios = totalComentarios,
            Comentarios = lista.Select(k => new ComentarioViewModel
            {
                Id = k.Id,
                Cliente = Abreviar(k.Nombre, k.Apellido),
                Estrellas = k.Estrellas,
                Comentario = k.Comentario ?? "",
                Servicio = k.Servicio,
                Fecha = Local(k.FechaCreacion)
            }).ToList()
        };

        return View(vm);
    }
}