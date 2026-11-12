using DKaiza.Web.Dominio;

namespace DKaiza.Web.Models;

public sealed record FilaCitaDto(
    int Id, DateTime Inicio, DateTime Fin, decimal Precio,
    EstadoCita Estado, string Servicio, string Nombres, string Apellidos);

public class CitaItemViewModel
{
    public int Id { get; set; }
    public DateTime Inicio { get; set; }   // hora local del salón
    public DateTime Fin { get; set; }
    public int Duracion { get; set; }
    public string Servicio { get; set; } = "";
    public string Estilista { get; set; } = "";
    public decimal Precio { get; set; }
    public string Estado { get; set; } = "";
    public string EstadoClase { get; set; } = "pen";
}

public class MisCitasViewModel
{
    public DateTime Hoy { get; set; }      // fecha de hoy en hora del salón
    public List<CitaItemViewModel> Citas { get; set; } = new();
}