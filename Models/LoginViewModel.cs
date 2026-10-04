using System.ComponentModel.DataAnnotations;

namespace DKaiza.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool Recordarme { get; set; }
}