using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Models;

public class PerfilViewModel
{
    // Solo lectura (se cargan desde la base de datos)
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public DateTime? FechaRegistro { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede tener más de 150 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede tener más de 20 caracteres.")]
    [RegularExpression(@"^[0-9+\s-]{7,20}$", ErrorMessage = "Ingresa un teléfono válido.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";
}