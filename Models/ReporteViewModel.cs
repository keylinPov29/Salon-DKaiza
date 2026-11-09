namespace DKaiza.Web.Models;

public class ReporteUnificadoViewModel
{
    // Filtros
    public int? AnioSeleccionado { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Datos Anuales
    public string EstilistaTop { get; set; } = "N/A";
    public string ServicioTop { get; set; } = "N/A";
    public List<int> ServiciosPorMes { get; set; } = new();
    public List<RankingDto> ServiciosDelMes { get; set; } = new();
    public List<EstilistaResumenDto> TablaEstilistas { get; set; } = new();
    
    // Lista para las columnas dinámicas
    public List<string> NombresServiciosColumnas { get; set; } = new();

    // Datos para el PDF (Desglose real mes a mes)
    public List<DesgloseMesDto> DesgloseMensualReal { get; set; } = new();

    // Datos Ingresos
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal GranTotal { get; set; }
    public int CantidadServicios { get; set; }
    public List<TransaccionDto> Transacciones { get; set; } = new();
    
    // Años disponibles para el dropdown
    public List<int> AniosDisponibles { get; set; } = new();
}

public class RankingDto
{
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public double Porcentaje { get; set; }
}

public class EstilistaResumenDto
{
    public string Nombre { get; set; } = "";
    public string Iniciales { get; set; } = "";
    public int Total { get; set; }
    public decimal Ingresos { get; set; }
    public Dictionary<string, int> ServiciosRealizados { get; set; } = new();
}

public class TransaccionDto
{
    public int CitaId { get; set; }
    public DateTime FechaTransaccion { get; set; }
    public string Cliente { get; set; } = "";
    public string Servicio { get; set; } = "";
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "Efectivo"; 
}

public class DesgloseMesDto
{
    public string MesNombre { get; set; } = "";
    public int TotalCitas { get; set; }
    public decimal TotalIngresos { get; set; }
    public Dictionary<string, int> CantidadPorServicio { get; set; } = new();
}