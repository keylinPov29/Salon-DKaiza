namespace DKaiza.Web.Models;

// ───────────── Pantalla 1: Gestión de historial de clientes ─────────────

public class AdminClienteFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";

    // Citas completadas (asistencias) de toda la historia del cliente
    public int Asistencias { get; set; }

    // "alta", "media" o "baja"
    public string Frecuencia { get; set; } = "baja";

    public bool EsVip { get; set; }
}

public class AdminClientesViewModel
{
    // Indicadores de arriba
    public int TotalClientes { get; set; }
    public int ClientesVip { get; set; }
    public int TotalAsistencias { get; set; }
    public double PromedioPorCliente { get; set; }

    public List<AdminClienteFilaViewModel> Clientes { get; set; } = new();

    // Para dibujar la barra de asistencias en proporción
    public int MaxAsistencias { get; set; }

    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int PorPagina { get; set; } = 10;
}

// ───────────── Pantalla 2: Historial del cliente ─────────────

public class AdminClienteHistorialViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";

    public bool EsVip { get; set; }

    // "alta", "media" o "baja" (según las asistencias de los últimos 6 meses)
    public string Frecuencia { get; set; } = "baja";
    public int AsistenciasUltimos6Meses { get; set; }

    // Indicadores (dentro del rango de fechas elegido)
    public int TotalRegistros { get; set; }
    public int Asistencias { get; set; }
    public int Cancelaciones { get; set; }
    public int Ausencias { get; set; }

    // Filtro por rango de fechas (null = sin filtrar)
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    // Más reciente primero
    public List<HistorialClienteItemViewModel> Registros { get; set; } = new();

    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
}