namespace DKaiza.Web.Models;

/// <summary>Paso 1: datos iniciales del servicio seleccionado + calendario</summary>
public class ReservaPasoUnoViewModel
{
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }

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
    public decimal PrecioTotal { get; set; }
}
