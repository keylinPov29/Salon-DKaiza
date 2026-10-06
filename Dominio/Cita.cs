using System.ComponentModel.DataAnnotations;
using DKaiza.Web.Models;

namespace DKaiza.Web.Dominio;

public enum EstadoCita
{
    Pendiente = 1,      // Reservada, pendiente de pago/confirmación
    Confirmada = 2,     // Pagada y confirmada
    Cancelada = 3,      // Cancelada por cliente o salón
    Completada = 4      // Servicio prestado
}

public class Cita
{
    public int Id { get; set; }

    // Cliente
    public int ClienteId { get; set; }
    public Usuario? Cliente { get; set; }

    // Servicio
    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    // Estilista
    public int EstilistaId { get; set; }
    public Estilista? Estilista { get; set; }

    // Fecha y hora de la cita (UTC)
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    // Precio capturado al momento de reservar
    public decimal PrecioTotal { get; set; }

    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;

    public DateTime FechaReserva { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? Notas { get; set; }
}
