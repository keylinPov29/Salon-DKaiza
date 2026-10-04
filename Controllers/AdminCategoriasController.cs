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
    [Route("admin/categorias")]
    public class AdminCategoriasController : Controller
    {
        private const long MaxImagenBytes = 2 * 1024 * 1024; // 2 MB
        private readonly ApplicationDbContext _db;

        public AdminCategoriasController(ApplicationDbContext db) => _db = db;

        // GET /admin/categorias?categoriaId=2
        [HttpGet("")]
        public async Task<IActionResult> Index(int? categoriaId)
            => View(await ConstruirCatalogoAsync(categoriaId));

        // GET /admin/categorias/nueva
        [HttpGet("nueva")]
        public IActionResult Nueva() => View("Formulario", new CategoriaFormViewModel());

        // POST /admin/categorias
        [HttpPost("")]
        public async Task<IActionResult> Crear(CategoriaFormViewModel vm)
        {
            await ValidarNombreAsync(vm);
            var (bytes, tipo) = await LeerImagenAsync(vm.Imagen);

            if (!ModelState.IsValid) return View("Formulario", vm);

            var cat = new CategoriaServicio
            {
                Nombre = vm.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim(),
                Imagen = bytes,
                ImagenTipo = tipo,
                UsuarioRegistroId = UsuarioActualId(),
                FechaRegistro = DateTime.UtcNow
            };

            _db.CategoriasServicio.Add(cat);
            if (!await GuardarAsync(vm)) return View("Formulario", vm);

            TempData["Ok"] = "Categoría creada correctamente.";
            return RedirectToAction(nameof(Index), new { categoriaId = cat.Id });
        }

        // GET /admin/categorias/{id}/editar
        [HttpGet("{id:int}/editar")]
        public async Task<IActionResult> Editar(int id)
        {
            var cat = await _db.CategoriasServicio.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            return View("Formulario", new CategoriaFormViewModel
            {
                Id = cat.Id,
                Nombre = cat.Nombre,
                Descripcion = cat.Descripcion,
                TieneImagen = cat.Imagen != null
            });
        }

        // POST /admin/categorias/{id}/editar
        [HttpPost("{id:int}/editar")]
        public async Task<IActionResult> Editar(int id, CategoriaFormViewModel vm)
        {
            var cat = await _db.CategoriasServicio.FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            vm.Id = id;
            vm.TieneImagen = cat.Imagen != null;

            await ValidarNombreAsync(vm);
            var (bytes, tipo) = await LeerImagenAsync(vm.Imagen);

            if (!ModelState.IsValid) return View("Formulario", vm);

            cat.Nombre = vm.Nombre.Trim();
            cat.Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim();
            if (bytes != null)
            {
                cat.Imagen = bytes;
                cat.ImagenTipo = tipo;
            }
            cat.UsuarioModificaId = UsuarioActualId();
            cat.FechaModifica = DateTime.UtcNow;

            if (!await GuardarAsync(vm)) return View("Formulario", vm);

            TempData["Ok"] = "Categoría actualizada correctamente.";
            return RedirectToAction(nameof(Index), new { categoriaId = id });
        }

        // POST /admin/categorias/{id}/eliminar  (409 si tiene servicios)
        [HttpPost("{id:int}/eliminar")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cat = await _db.CategoriasServicio.FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            if (await _db.Servicios.AnyAsync(s => s.CategoriaId == id))
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                ViewBag.Error = $"No se puede eliminar \"{cat.Nombre}\" porque tiene servicios asociados.";
                return View(nameof(Index), await ConstruirCatalogoAsync(id));
            }

            _db.CategoriasServicio.Remove(cat);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Categoría eliminada.";
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/categorias/{id}/imagen
        [HttpGet("{id:int}/imagen")]
        public async Task<IActionResult> Imagen(int id)
        {
            var cat = await _db.CategoriasServicio.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new { c.Imagen, c.ImagenTipo })
                .FirstOrDefaultAsync();

            if (cat?.Imagen == null) return NotFound();
            return File(cat.Imagen, cat.ImagenTipo ?? "image/jpeg");
        }

        // ---------- helpers ----------

        private int UsuarioActualId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<CatalogoViewModel> ConstruirCatalogoAsync(int? categoriaId)
        {
            var categorias = await _db.CategoriasServicio.AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaListaItem
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    TieneImagen = c.Imagen != null,
                    CantidadServicios = c.Servicios.Count
                })
                .ToListAsync();

            var seleccionada = categorias.FirstOrDefault(c => c.Id == categoriaId)
                               ?? categorias.FirstOrDefault();

            var servicios = new List<ServicioListaItem>();
            if (seleccionada != null)
            {
                servicios = await _db.Servicios.AsNoTracking()
                    .Where(s => s.CategoriaId == seleccionada.Id)
                    .OrderBy(s => s.Nombre)
                     .Select(s => new ServicioListaItem
                    {
                        Id = s.Id,
                        Nombre = s.Nombre,
                        DuracionMinutos = s.DuracionMinutos,
                        Precio = s.Precio,
                        Activo = s.Activo,
                        TieneImagen = s.Imagen != null
                    })
                    .ToListAsync();
            }

            return new CatalogoViewModel
            {
                Categorias = categorias,
                Seleccionada = seleccionada,
                Servicios = servicios
            };
        }

        private async Task ValidarNombreAsync(CategoriaFormViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Nombre)) return;

            var nombre = vm.Nombre.Trim().ToLower();
            var existe = await _db.CategoriasServicio
                .AnyAsync(c => c.Id != vm.Id && c.Nombre.ToLower() == nombre);

            if (existe)
                ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una categoría con ese nombre.");
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

            // Se valida por el contenido real, no por la extensión.
            bool esPng = b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
            bool esJpg = b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

            if (esPng) return (b, "image/png");
            if (esJpg) return (b, "image/jpeg");

            ModelState.AddModelError("Imagen", "Solo se permiten imágenes PNG o JPG.");
            return (null, null);
        }

        private async Task<bool> GuardarAsync(CategoriaFormViewModel vm)
        {
            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una categoría con ese nombre.");
                return false;
            }
        }
    }
}