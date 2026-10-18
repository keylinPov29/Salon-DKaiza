using DKaiza.Web.Dominio;

namespace DKaiza.Web.Estilistas;

public class AgendaEstilistaViewModel
{
    // DateTime? permite que la fecha esté vacía (para mostrar todas las citas futuras)
    public DateTime? FechaSeleccionada { get; set; } 
    public int? EstilistaIdFiltro { get; set; }
    public bool EsAdmin { get; set; }
    public List<Estilista> ListaEstilistas { get; set; } = new();
    public List<CitaDto> Citas { get; set; } = new();
}

public class CitaDto
{
    public int Id { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteTelefono { get; set; } = "";
    public string ServicioNombre { get; set; } = "";
    public string EstilistaNombre { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public decimal PrecioTotal { get; set; }
    public EstadoCita Estado { get; set; }
    public string? Notas { get; set; }
}

public class DetalleCitaDto
{
    public int Id { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteTelefono { get; set; } = "";
    public string ServicioNombre { get; set; } = "";
    public string EstilistaNombre { get; set; } = "";
    public string FechaStr { get; set; } = "";
    public string HorarioStr { get; set; } = "";
    public decimal PrecioTotal { get; set; }
    public EstadoCita Estado { get; set; }
    public string? Notas { get; set; }
}