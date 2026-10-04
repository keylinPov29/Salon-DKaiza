using DKaiza.Data;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) => _db = db;

        // Página pública: no requiere iniciar sesión.
        public async Task<IActionResult> Index()
        {
            // Solo categorías con al menos un servicio activo, y solo sus servicios activos.
            var datos = await _db.CategoriasServicio.AsNoTracking()
                .Where(c => c.Servicios.Any(s => s.Activo))
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Descripcion,
                    TieneImagen = c.Imagen != null,
                    Servicios = c.Servicios
                        .Where(s => s.Activo)
                        .OrderBy(s => s.Nombre)
                        .Select(s => new
                        {
                            s.Id,
                            s.Nombre,
                            s.DuracionMinutos,
                            s.Precio,
                            TieneImagen = s.Imagen != null
                        })
                        .ToList()
                })
                .ToListAsync();

            // Ids con imagen: la vista las usa en lugar del ícono o junto al nombre.
            ViewBag.CategoriasConImagen = datos.Where(c => c.TieneImagen).Select(c => c.Id).ToHashSet();
            ViewBag.ServiciosConImagen = datos.SelectMany(c => c.Servicios)
                .Where(s => s.TieneImagen).Select(s => s.Id).ToHashSet();

            var modelo = new Servicio
            {
                Categorias = datos.Select(c => new Categoria
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion ?? "",
                    Servicios = c.Servicios.Select(s => new ItemServicio
                    {
                        Id = s.Id,
                        Nombre = s.Nombre,
                        DuracionMinutos = s.DuracionMinutos,
                        Precio = s.Precio
                    }).ToList()
                }).ToList()
            };

            return View(modelo);
        }
    }
}