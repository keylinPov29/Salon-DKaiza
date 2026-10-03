using System.Collections.Generic;
using System.Linq;

namespace DKaiza.Web.Models
{
    public class Servicio
    {
        public List<Categoria> Categorias { get; set; } = new();

        public bool HayServicios => Categorias != null && Categorias.Any(c => c.Servicios.Any());
    }

    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public List<ItemServicio> Servicios { get; set; } = new();
    }

    public class ItemServicio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
    }
}