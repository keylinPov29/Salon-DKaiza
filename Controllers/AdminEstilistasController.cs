using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DKaiza.Data;
using DKaiza.Web.Aplicacion.Estilistas;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Administrador")]
[Route("admin/estilistas")]
public class AdminEstilistasController : Controller
{
    private const int MaxFotoBytes = 2 * 1024 * 1024;
    private readonly ApplicationDbContext _db;

    public AdminEstilistasController(ApplicationDbContext db) => _db = db;

    // ---------- Lista de Estilistas ----------
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var filas = await _db.Estilistas.AsNoTracking()
            .OrderByDescending(e => e.Activo).ThenBy(e => e.Nombres).ThenBy(e => e.Apellidos)
            .Select(e => new
            {
                e.Id, e.Nombres, e.Apellidos, e.Especialidad, e.Telefono, e.Dni,
                e.InicioJornada, e.FinJornada, e.InicioDescanso, e.FinDescanso,
                e.Activo, TieneFoto = e.Foto != null
            })
            .ToListAsync();

        var lista = filas.Select(e => new EstilistaListaItem
        {
            Id = e.Id,
            NombreCompleto = $"{e.Nombres} {e.Apellidos}".Trim(),
            Iniciales = $"{e.Nombres.FirstOrDefault()}{e.Apellidos.FirstOrDefault()}".ToUpper(),
            Especialidad = e.Especialidad,
            Telefono = e.Telefono,
            Dni = e.Dni,
            Jornada = $"{e.InicioJornada:HH\\:mm}–{e.FinJornada:HH\\:mm}",
            Descanso = e.InicioDescanso.HasValue && e.FinDescanso.HasValue
                ? $"{e.InicioDescanso:HH\\:mm}–{e.FinDescanso:HH\\:mm}" : null,
            Activo = e.Activo,
            TieneFoto = e.TieneFoto
        }).ToList();

        return View(lista);
    }

    // ---------- Añadir Estilista ----------
    [HttpGet("nuevo")]
    public async Task<IActionResult> Nuevo()
    {
        var vm = new EstilistaFormViewModel();
        await CargarEspecialidadesAsync(vm);
        return View("Formulario", vm);
    }

    [HttpPost("nuevo")]
    public async Task<IActionResult> Crear(EstilistaFormViewModel vm)
    {
        await CargarEspecialidadesAsync(vm);
        vm.Dni = (vm.Dni ?? "").Trim().ToUpper();

        await ValidarAsync(vm, null);
        var (bytes, tipo) = await LeerFotoAsync(vm);
        if (!ModelState.IsValid) return View("Formulario", vm);

        // Las 3 estilistas iniciales tienen Id fijo: se ajusta la secuencia para que la nueva no choque.
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT setval(pg_get_serial_sequence('\"Estilistas\"', 'Id'), (SELECT COALESCE(MAX(\"Id\"), 1) FROM \"Estilistas\"))");

        var clave = GenerarClaveTemporal();
        var email = await GenerarCorreoAsync(vm.Nombres, vm.Apellidos);

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var estilista = new Estilista
            {
                Nombres = vm.Nombres.Trim(),
                Apellidos = vm.Apellidos.Trim(),
                Dni = vm.Dni,
                Telefono = vm.Telefono.Trim(),
                Especialidad = vm.Especialidad,
                InicioJornada = vm.InicioJornada!.Value,
                FinJornada = vm.FinJornada!.Value,
                InicioDescanso = vm.InicioDescanso,
                FinDescanso = vm.FinDescanso,
                Foto = bytes,
                FotoTipo = tipo,
                Activo = true,
                UsuarioRegistroId = UsuarioActualId(),
                FechaRegistro = DateTime.UtcNow
            };
            _db.Estilistas.Add(estilista);
            await _db.SaveChangesAsync();

            _db.Usuarios.Add(new Usuario
            {
                Nombre = estilista.Nombres,
                Apellido = estilista.Apellidos,
                Email = email,
                Telefono = estilista.Telefono,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave),
                Rol = RolUsuario.Estilista,
                Activo = true,
                DebeCambiarPassword = true,
                EstilistaId = estilista.Id,
                FechaRegistro = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se pudo registrar al estilista. Revisa que el documento no esté repetido.");
            return View("Formulario", vm);
        }

        TempData["Success"] = "Estilista registrado correctamente.";
        TempData["CredEmail"] = email;
        TempData["CredClave"] = clave;
        return RedirectToAction(nameof(Index));
    }

    // ---------- Editar ----------
    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id)
    {
        var e = await _db.Estilistas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        var vm = new EstilistaFormViewModel
        {
            Id = e.Id, Nombres = e.Nombres, Apellidos = e.Apellidos, Dni = e.Dni,
            Telefono = e.Telefono, Especialidad = e.Especialidad,
            InicioJornada = e.InicioJornada, FinJornada = e.FinJornada,
            InicioDescanso = e.InicioDescanso, FinDescanso = e.FinDescanso,
            TieneFoto = e.Foto != null
        };
        await CargarEspecialidadesAsync(vm);
        return View("Formulario", vm);
    }

    [HttpPost("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id, EstilistaFormViewModel vm)
    {
        var e = await _db.Estilistas.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        vm.Id = id;
        vm.TieneFoto = e.Foto != null;
        vm.Dni = (vm.Dni ?? "").Trim().ToUpper();
        await CargarEspecialidadesAsync(vm);

        await ValidarAsync(vm, id);
        var (bytes, tipo) = await LeerFotoAsync(vm);
        if (!ModelState.IsValid) return View("Formulario", vm);

        e.Nombres = vm.Nombres.Trim();
        e.Apellidos = vm.Apellidos.Trim();
        e.Dni = vm.Dni;
        e.Telefono = vm.Telefono.Trim();
        e.Especialidad = vm.Especialidad;
        e.InicioJornada = vm.InicioJornada!.Value;
        e.FinJornada = vm.FinJornada!.Value;
        e.InicioDescanso = vm.InicioDescanso;
        e.FinDescanso = vm.FinDescanso;
        if (bytes != null) { e.Foto = bytes; e.FotoTipo = tipo; }
        e.UsuarioModificaId = UsuarioActualId();
        e.FechaModifica = DateTime.UtcNow;

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "No se pudo guardar. Revisa que el documento no esté repetido.");
            return View("Formulario", vm);
        }

        TempData["Success"] = "Estilista actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Desactivar (temporal) ----------
    [HttpPost("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var e = await _db.Estilistas.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        e.Activo = false;
        e.UsuarioModificaId = UsuarioActualId();
        e.FechaModifica = DateTime.UtcNow;

        // También se bloquea su cuenta de acceso
        var cuentas = await _db.Usuarios.Where(u => u.EstilistaId == id).ToListAsync();
        foreach (var u in cuentas) u.Activo = false;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Estilista desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id)
    {
        var e = await _db.Estilistas.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        e.Activo = true;
        e.UsuarioModificaId = UsuarioActualId();
        e.FechaModifica = DateTime.UtcNow;

        var cuentas = await _db.Usuarios.Where(u => u.EstilistaId == id).ToListAsync();
        foreach (var u in cuentas) u.Activo = true;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Estilista activado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Eliminar (definitivo) ----------
    [HttpPost("{id:int}/eliminar")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var e = await _db.Estilistas.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();

        // TODO (cuando exista la tabla de citas): si tiene citas pendientes o futuras, responder 409 y no eliminar.

        try
        {
            // Se elimina también su cuenta de acceso
            var cuentas = await _db.Usuarios.Where(u => u.EstilistaId == id).ToListAsync();
            _db.Usuarios.RemoveRange(cuentas);
            _db.Estilistas.Remove(e);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Tiene historial (por ejemplo citas) que impide borrarlo
            TempData["Error"] = "No se puede eliminar: el estilista tiene historial asociado. Desactívalo en su lugar.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Estilista eliminado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Foto ----------
    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> Foto(int id)
    {
        var f = await _db.Estilistas.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Foto, x.FotoTipo })
            .FirstOrDefaultAsync();
        if (f?.Foto == null) return NotFound();
        return File(f.Foto, f.FotoTipo ?? "image/jpeg");
    }

    // ---------- Ayudas ----------
    private int? UsuarioActualId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private async Task CargarEspecialidadesAsync(EstilistaFormViewModel vm)
    {
        vm.Especialidades = await _db.CategoriasServicio.AsNoTracking()
            .OrderBy(c => c.Nombre).Select(c => c.Nombre).ToListAsync();

        // Si la especialidad guardada ya no existe en el catálogo, se conserva para no perderla al editar
        if (!string.IsNullOrWhiteSpace(vm.Especialidad) && !vm.Especialidades.Contains(vm.Especialidad))
            vm.Especialidades.Add(vm.Especialidad);
    }

    private async Task ValidarAsync(EstilistaFormViewModel vm, int? idActual)
    {
        // La jornada debe estar dentro del horario del salón
        var aperturaSalon = new TimeOnly(9, 30);
        var cierreSalon = new TimeOnly(21, 0);
        if (vm.InicioJornada.HasValue && vm.InicioJornada < aperturaSalon)
            ModelState.AddModelError(nameof(vm.InicioJornada), "El salón abre a las 9:30. La jornada no puede empezar antes.");
        if (vm.FinJornada.HasValue && vm.FinJornada > cierreSalon)
            ModelState.AddModelError(nameof(vm.FinJornada), "El salón cierra a las 21:00. La jornada no puede terminar después.");

        if (!string.IsNullOrWhiteSpace(vm.Dni) &&
            await _db.Estilistas.AnyAsync(x => x.Dni == vm.Dni && x.Id != (idActual ?? 0)))
            ModelState.AddModelError(nameof(vm.Dni), "Ya existe un estilista con ese documento de identidad.");

        if (vm.InicioJornada.HasValue && vm.FinJornada.HasValue && vm.FinJornada <= vm.InicioJornada)
            ModelState.AddModelError(nameof(vm.FinJornada), "El fin de la jornada debe ser posterior al inicio.");

        var conInicio = vm.InicioDescanso.HasValue;
        var conFin = vm.FinDescanso.HasValue;
        if (conInicio != conFin)
            ModelState.AddModelError(nameof(vm.FinDescanso), "Completa el inicio y el fin del descanso, o deja ambos vacíos.");
        else if (conInicio && vm.FinDescanso <= vm.InicioDescanso)
            ModelState.AddModelError(nameof(vm.FinDescanso), "El fin del descanso debe ser posterior al inicio.");
        else if (conInicio && vm.InicioJornada.HasValue && vm.FinJornada.HasValue &&
                 (vm.InicioDescanso < vm.InicioJornada || vm.FinDescanso > vm.FinJornada))
            ModelState.AddModelError(nameof(vm.InicioDescanso), "El descanso debe estar dentro de la jornada.");
    }

    // Solo PNG o JPG, máximo 2 MB (se comprueba el contenido real, no solo la extensión)
    private async Task<(byte[]? bytes, string? tipo)> LeerFotoAsync(EstilistaFormViewModel vm)
    {
        if (vm.Foto == null || vm.Foto.Length == 0) return (null, null);

        if (vm.Foto.Length > MaxFotoBytes)
        {
            ModelState.AddModelError(nameof(vm.Foto), "La foto no puede pesar más de 2 MB.");
            return (null, null);
        }

        using var ms = new MemoryStream();
        await vm.Foto.CopyToAsync(ms);
        var b = ms.ToArray();

        if (b.Length > 3 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            return (b, "image/png");
        if (b.Length > 2 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return (b, "image/jpeg");

        ModelState.AddModelError(nameof(vm.Foto), "La foto debe ser PNG o JPG.");
        return (null, null);
    }

    // nombre.apellido@dkaiza.com (sin tildes); si ya existe, agrega un número
    private async Task<string> GenerarCorreoAsync(string nombres, string apellidos)
    {
        var baseNombre = Limpiar(nombres.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "estilista");
        var baseApellido = Limpiar(apellidos.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "dkaiza");
        var correo = $"{baseNombre}.{baseApellido}@dkaiza.com";

        var n = 2;
        while (await _db.Usuarios.AnyAsync(u => u.Email == correo))
            correo = $"{baseNombre}.{baseApellido}{n++}@dkaiza.com";
        return correo;
    }

    private static string Limpiar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.Length > 0 ? sb.ToString() : "estilista";
    }

    // Clave temporal aleatoria de 10 caracteres (mayúscula, minúscula, número y símbolo)
    private static string GenerarClaveTemporal()
    {
        const string May = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string Min = "abcdefghijkmnpqrstuvwxyz";
        const string Num = "23456789";
        const string Sim = "#$%&*?";
        var todo = May + Min + Num + Sim;

        var chars = new List<char>
        {
            May[RandomNumberGenerator.GetInt32(May.Length)],
            Min[RandomNumberGenerator.GetInt32(Min.Length)],
            Num[RandomNumberGenerator.GetInt32(Num.Length)],
            Sim[RandomNumberGenerator.GetInt32(Sim.Length)]
        };
        while (chars.Count < 10) chars.Add(todo[RandomNumberGenerator.GetInt32(todo.Length)]);

        // Mezcla
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
}