using DKaiza.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers
{
    [AllowAnonymous]
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MediaController(ApplicationDbContext db) => _db = db;

        // GET /media/categorias/{id}
        [HttpGet("categorias/{id:int}")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> Categoria(int id)
        {
            var c = await _db.CategoriasServicio.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Imagen, x.ImagenTipo })
                .FirstOrDefaultAsync();

            if (c?.Imagen == null) return NotFound();
            return File(c.Imagen, c.ImagenTipo ?? "image/jpeg");
        }

        // GET /media/servicios/{id}
        [HttpGet("servicios/{id:int}")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> Servicio(int id)
        {
            var s = await _db.Servicios.AsNoTracking()
                .Where(x => x.Id == id && x.Activo)
                .Select(x => new { x.Imagen, x.ImagenTipo })
                .FirstOrDefaultAsync();

            if (s?.Imagen == null) return NotFound();
            return File(s.Imagen, s.ImagenTipo ?? "image/jpeg");
        }
    }
}