using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DKaiza.Web.Aplicacion.Catalogo
{
    // ---------- Pantalla del catálogo ----------

    public class CategoriaListaItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public bool TieneImagen { get; set; }
        public int CantidadServicios { get; set; }
    }

    public class ServicioListaItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; }
        public bool TieneImagen { get; set; }
    }

    public class CatalogoViewModel
    {
        public List<CategoriaListaItem> Categorias { get; set; } = new();
        public CategoriaListaItem? Seleccionada { get; set; }
        public List<ServicioListaItem> Servicios { get; set; } = new();
    }

    // ---------- Formulario de categoría ----------

    public class CategoriaFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, ErrorMessage = "El nombre admite máximo 80 caracteres.")]
        public string Nombre { get; set; } = "";

        [StringLength(300, ErrorMessage = "La descripción admite máximo 300 caracteres.")]
        public string? Descripcion { get; set; }

        public IFormFile? Imagen { get; set; }

        public bool TieneImagen { get; set; }
    }

    // ---------- Formulario de servicio ----------

    public class CategoriaOpcion
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
    }

    public class ServicioFormViewModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
        public int CategoriaId { get; set; }

        public List<CategoriaOpcion> Categorias { get; set; } = new();

        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite máximo 100 caracteres.")]
        public string Nombre { get; set; } = "";

        [StringLength(300, ErrorMessage = "La descripción admite máximo 300 caracteres.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(1, 1440, ErrorMessage = "La duración debe ser mayor que 0 minutos.")]
        public int? DuracionMinutos { get; set; }

        [Required(ErrorMessage = "El costo es obligatorio.")]
        [Range(0.01, 99999999.99, ErrorMessage = "El costo debe ser mayor que 0.")]
        public decimal? Precio { get; set; }

        public bool Activo { get; set; } = true;

        public IFormFile? Imagen { get; set; }

        public bool TieneImagen { get; set; }
    }
}