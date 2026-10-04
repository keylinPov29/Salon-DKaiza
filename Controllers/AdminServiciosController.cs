using System.Security.Claims;
using DKaiza.Data;
using DKaiza.Web.Aplicacion.Catalogo;
using DKaiza.Web.Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DKaiza.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    [Route("admin/servicios")]
    public class AdminServiciosController : Controller
    {
        private const long MaxImagenBytes = 2 * 1024 * 1024; // 2 MB
        private readonly ApplicationDbContext _db;

        public AdminServiciosController(ApplicationDbContext db) => _db = db;

        // GET /admin/servicios  -> el listado vive en el catálogo
        [HttpGet("")]
        public IActionResult Index() => RedirectToAction("Index", "AdminCategorias");

        // GET /admin/servicios/nuevo?categoriaId=1
        [HttpGet("nuevo")]
        public async Task<IActionResult> Nuevo(int? categoriaId)
        {
            var vm = new ServicioFormViewModel();
            await CargarCategoriasAsync(vm);

            if (vm.Categorias.Count == 0)
            {
                TempData["Error"] = "Primero crea una categoría.";
                return RedirectToAction("Index", "AdminCategorias");
            }

            vm.CategoriaId = vm.Categorias.Any(c => c.Id == categoriaId)
                ? categoriaId!.Value
                : vm.Categorias[0].Id;

            return View("Formulario", vm);
        }

        // POST /admin/servicios
        [HttpPost("")]
        public async Task<IActionResult> Crear(ServicioFormViewModel vm)
        {
            await CargarCategoriasAsync(vm);
            if (!vm.Categorias.Any(c => c.Id == vm.CategoriaId))
                ModelState.AddModelError(nameof(vm.CategoriaId), "Selecciona una categoría válida.");

            var (bytes, tipo) = await LeerImagenAsync(vm.Imagen);
            if (!ModelState.IsValid) return View("Formulario", vm);

            var servicio = new Servicio
            {
                CategoriaId = vm.CategoriaId,
                Nombre = vm.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim(),
                DuracionMinutos = vm.DuracionMinutos!.Value,
                Precio = Math.Round(vm.Precio!.Value, 2),
                Imagen = bytes,
                ImagenTipo = tipo,
                Activo = vm.Activo,
                UsuarioRegistroId = UsuarioActualId(),
                FechaRegistro = DateTime.UtcNow
            };

            _db.Servicios.Add(servicio);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Servicio registrado correctamente.";
            return RedirectToAction("Index", "AdminCategorias", new { categoriaId = vm.CategoriaId });
        }

        // GET /admin/servicios/{id}/editar
        [HttpGet("{id:int}/editar")]
        public async Task<IActionResult> Editar(int id)
        {
            var s = await _db.Servicios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var vm = new ServicioFormViewModel
            {
                Id = s.Id,
                CategoriaId = s.CategoriaId,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                DuracionMinutos = s.DuracionMinutos,
                Precio = s.Precio,
                Activo = s.Activo,
                TieneImagen = s.Imagen != null
            };
            await CargarCategoriasAsync(vm);

            return View("Formulario", vm);
        }

        // POST /admin/servicios/{id}/editar
        [HttpPost("{id:int}/editar")]
        public async Task<IActionResult> Editar(int id, ServicioFormViewModel vm)
        {
            var s = await _db.Servicios.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            vm.Id = id;
            vm.TieneImagen = s.Imagen != null;
            await CargarCategoriasAsync(vm);

            if (!vm.Categorias.Any(c => c.Id == vm.CategoriaId))
                ModelState.AddModelError(nameof(vm.CategoriaId), "Selecciona una categoría válida.");

            var (bytes, tipo) = await LeerImagenAsync(vm.Imagen);
            if (!ModelState.IsValid) return View("Formulario", vm);

            s.CategoriaId = vm.CategoriaId;
            s.Nombre = vm.Nombre.Trim();
            s.Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim();
            s.DuracionMinutos = vm.DuracionMinutos!.Value;
            s.Precio = Math.Round(vm.Precio!.Value, 2);
            s.Activo = vm.Activo;
            if (bytes != null)
            {
                s.Imagen = bytes;
                s.ImagenTipo = tipo;
            }
            s.UsuarioModificaId = UsuarioActualId();
            s.FechaModifica = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Ok"] = "Servicio actualizado correctamente.";
            return RedirectToAction("Index", "AdminCategorias", new { categoriaId = s.CategoriaId });
        }

        // POST /admin/servicios/{id}/estado  (activar / desactivar)
        [HttpPost("{id:int}/estado")]
        public async Task<IActionResult> Estado(int id)
        {
            var s = await _db.Servicios.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            s.Activo = !s.Activo;
            s.UsuarioModificaId = UsuarioActualId();
            s.FechaModifica = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Ok"] = s.Activo ? "Servicio activado." : "Servicio desactivado.";
            return RedirectToAction("Index", "AdminCategorias", new { categoriaId = s.CategoriaId });
        }

        // POST /admin/servicios/{id}/eliminar
        [HttpPost("{id:int}/eliminar")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var s = await _db.Servicios.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var categoriaId = s.CategoriaId;
            _db.Servicios.Remove(s);

            try
            {
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Servicio eliminado.";
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
            {
                // Más adelante: el servicio ya tiene citas asociadas.
                TempData["Error"] = "No se puede eliminar un servicio con citas asociadas. Desactívalo en su lugar.";
            }

            return RedirectToAction("Index", "AdminCategorias", new { categoriaId });
        }

        // GET /admin/servicios/{id}/imagen
        [HttpGet("{id:int}/imagen")]
        public async Task<IActionResult> Imagen(int id)
        {
            var s = await _db.Servicios.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Imagen, x.ImagenTipo })
                .FirstOrDefaultAsync();

            if (s?.Imagen == null) return NotFound();
            return File(s.Imagen, s.ImagenTipo ?? "image/jpeg");
        }

        // ---------- helpers ----------

        private int UsuarioActualId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task CargarCategoriasAsync(ServicioFormViewModel vm)
        {
            vm.Categorias = await _db.CategoriasServicio.AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaOpcion { Id = c.Id, Nombre = c.Nombre })
                .ToListAsync();
        }

        private async Task<(byte[]? bytes, string? tipo)> LeerImagenAsync(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0) return (null, null);

            if (archivo.Length > MaxImagenBytes)
            {
                ModelState.AddModelError("Imagen", "La imagen no puede pesar más de 2 MB.");
                return (null, null);
            }

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var b = ms.ToArray();

            bool esPng = b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
            bool esJpg = b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

            if (esPng) return (b, "image/png");
            if (esJpg) return (b, "image/jpeg");

            ModelState.AddModelError("Imagen", "Solo se permiten imágenes PNG o JPG.");
            return (null, null);
        }
    }
}