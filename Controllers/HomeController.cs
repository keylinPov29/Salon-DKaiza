using Microsoft.AspNetCore.Mvc;
using DKaiza.Web.Models;

namespace DKaiza.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var modelo = new Servicio
            {
                Categorias = new List<Categoria>
                {
                    new Categoria
                    {
                        Id = 1,
                        Nombre = "Cabello",
                        Descripcion = "Cortes y cuidado para tu cabello",
                        Servicios = new List<ItemServicio>
                        {
                            new() { Id = 1, Nombre = "Corte de cabello", DuracionMinutos = 45, Precio = 35.00m },
                            new() { Id = 2, Nombre = "Lavado y peinado", DuracionMinutos = 40, Precio = 30.00m },
                            new() { Id = 3, Nombre = "Peinado especial", DuracionMinutos = 60, Precio = 50.00m }
                        }
                    },
                    new Categoria
                    {
                        Id = 2,
                        Nombre = "Manicure",
                        Descripcion = "Cuidado y belleza para tus manos",
                        Servicios = new List<ItemServicio>
                        {
                            new() { Id = 4, Nombre = "Manicure clásica", DuracionMinutos = 40, Precio = 25.00m },
                            new() { Id = 5, Nombre = "Manicure semipermanente", DuracionMinutos = 60, Precio = 45.00m },
                            new() { Id = 6, Nombre = "Diseño de uñas", DuracionMinutos = 30, Precio = 20.00m }
                        }
                    },
                    new Categoria
                    {
                        Id = 3,
                        Nombre = "Coloración",
                        Descripcion = "Color y transformación para tu cabello",
                        Servicios = new List<ItemServicio>
                        {
                            new() { Id = 7, Nombre = "Tinte completo", DuracionMinutos = 120, Precio = 90.00m },
                            new() { Id = 8, Nombre = "Mechas", DuracionMinutos = 150, Precio = 120.00m },
                            new() { Id = 9, Nombre = "Balayage", DuracionMinutos = 180, Precio = 150.00m }
                        }
                    }
                }
            };

            return View(modelo);
        }
    }
}