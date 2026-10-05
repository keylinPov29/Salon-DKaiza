using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DKaiza.Web.Aplicacion.Estilistas;

// Una fila de la "Lista de Estilistas"
public class EstilistaListaItem
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Iniciales { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Jornada { get; set; } = "";
    public string? Descanso { get; set; }
    public bool Activo { get; set; }
    public bool TieneFoto { get; set; }
}

// Formulario "Registro de estilista" (crear y editar)
public class EstilistaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresa los nombres.")]
    [StringLength(100)]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "Ingresa los apellidos.")]
    [StringLength(100)]
    public string Apellidos { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el documento de identidad.")]
    [RegularExpression(@"^[0-9A-Za-z]{6,12}$", ErrorMessage = "El documento debe tener entre 6 y 12 letras o números, sin espacios.")]
    public string Dni { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el teléfono de contacto.")]
    [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Ingresa un teléfono válido (solo números, espacios o +).")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Selecciona la especialidad.")]
    [StringLength(150)]
    public string Especialidad { get; set; } = "";

    // Por defecto, el horario del salón: 9:30–21:00, con cierre al mediodía de 13:00 a 15:00
    [Required(ErrorMessage = "Indica el inicio de la jornada.")]
    public TimeOnly? InicioJornada { get; set; } = new TimeOnly(9, 30);

    [Required(ErrorMessage = "Indica el fin de la jornada.")]
    public TimeOnly? FinJornada { get; set; } = new TimeOnly(21, 0);

    public TimeOnly? InicioDescanso { get; set; } = new TimeOnly(13, 0);
    public TimeOnly? FinDescanso { get; set; } = new TimeOnly(15, 0);

    // Opciones del desplegable (nombres de las categorías del catálogo)
    public List<string> Especialidades { get; set; } = new();

    // "Adjuntar foto"
    public IFormFile? Foto { get; set; }
    public bool TieneFoto { get; set; }
}