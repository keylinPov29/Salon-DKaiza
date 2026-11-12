namespace DKaiza.Web.Models;

/// <summary>Paso 1: datos iniciales del servicio seleccionado + calendario</summary>
public class ReservaPasoUnoViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }

    // Reprogramación (HU-03): id de la cita que se está moviendo y su resumen para el aviso
    public int? ReprogramarId { get; set; }
    public string? ReprogramarResumen { get; set; }


    // Días disponibles desde mañana hasta el final del segundo mes siguiente.
    public List<DateOnly> DiasDisponibles { get; set; } = new();
    public DateOnly FechaMinima { get; set; }
    public DateOnly FechaMaxima { get; set; }
}

/// <summary>Paso 2: estilistas disponibles para la fecha elegida</summary>
public class ReservaPasoDosViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public DateOnly Fecha { get; set; }

    // Reprogramación (HU-03)
    public int? ReprogramarId { get; set; }
    public string? ReprogramarResumen { get; set; }

    public List<EstilistaDisponibleDto> Estilistas { get; set; } = new();
}

public class EstilistaDisponibleDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public bool TieneFoto { get; set; }
    public int HorariosLibres { get; set; }  // cantidad de slots disponibles ese día
}

/// <summary>Paso 3: horarios disponibles para estilista + fecha elegidos</summary>
public class ReservaPasoTresViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public DateOnly Fecha { get; set; }
    public int EstilistaId { get; set; }
    public string EstilistaNombre { get; set; } = "";
    public string EstilistaEspecialidad { get; set; } = "";

     // Reprogramación (HU-03)
    public int? ReprogramarId { get; set; }
    public string? ReprogramarResumen { get; set; }

    // Hora de la cita que se está moviendo (mismo día y estilista); se muestra deshabilitada
    public TimeOnly? HoraActual { get; set; }

    // Slots de tiempo disponibles (en hora local del salón)
    public List<TimeOnly> HorariosDisponibles { get; set; } = new();
}

/// <summary>Resumen de la cita confirmada (paso 4)</summary>
public class ReservaResumenViewModel
{
    public int CitaId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public string EstilistaNombre { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal Descuento { get; set; }
    public decimal PrecioTotal { get; set; }
    public string? CuponCodigo { get; set; }
    public string? MetodoPago { get; set; }
    public string? NumeroOperacion { get; set; }
    public string Estado { get; set; } = "";
    public bool PagoEnProceso { get; set; }
    public string MercadoPagoPublicKey { get; set; } = "";
    public string ClienteEmail { get; set; } = "";
}

public class PagoTarjetaRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(400)]
    public string Token { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
    public string PaymentMethodId { get; set; } = "";

    [System.ComponentModel.DataAnnotations.StringLength(50)]
    public string? IssuerId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 36)]
    public int Installments { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(10)]
    public string IdentificationType { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(30)]
    public string IdentificationNumber { get; set; } = "";
}

public class NotificacionesClienteViewModel
{
    public List<NotificacionClienteDto> Recordatorios { get; set; } = new();
}

public class NotificacionClienteDto
{
    public int NotificacionId { get; set; }
    public int CitaId { get; set; }
    public string Servicio { get; set; } = "";
    public string Estilista { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public DateTime CreadaEn { get; set; }
    public bool AsistenciaConfirmada { get; set; }
    public decimal Precio { get; set; }
    public string EstadoPago { get; set; } = "";
}