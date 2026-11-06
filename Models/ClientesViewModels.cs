namespace DKaiza.Web.Models;

// ───────────── Pantalla 1: Mis clientes / buscar clientes ─────────────

public class ClienteFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";

    // Citas completadas (asistencias)
    public int Visitas { get; set; }

    // Servicio que más ha pedido (null si aún no tiene visitas)
    public string? Preferencia { get; set; }

    // Fecha local de la última visita completada (null si no tiene)
    public DateTime? UltimaVisita { get; set; }

    // Fecha local de su próxima cita no cancelada (null si no tiene)
    public DateTime? ProximaCita { get; set; }
}

public class ClientesBuscarViewModel
{
    public string? Q { get; set; }
    public bool Buscado { get; set; }

    // true = lista por defecto "Mis clientes" (sin búsqueda)
    public bool MisClientes { get; set; }

    public List<ClienteFilaViewModel> Resultados { get; set; } = new();

    // Para dibujar la barra de visitas en proporción
    public int MaxVisitas { get; set; }

    public int Total { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int PorPagina { get; set; } = 10;
}

// ───────────── Pantalla 2: Ficha del cliente ─────────────

public class PreferenciaViewModel
{
    public string Servicio { get; set; } = "";
    public int Veces { get; set; }
}

public class NotaClienteViewModel
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public string Autor { get; set; } = "";

    // Fecha local del salón
    public DateTime Fecha { get; set; }
}

public class HistorialClienteItemViewModel
{
    public string Servicio { get; set; } = "";
    public string Estilista { get; set; } = "";

    // Fecha y hora local del salón
    public DateTime Inicio { get; set; }

    // "Asistió", "Canceló", "No asistió" o "Programada"
    public string EstadoTexto { get; set; } = "";

    // "ok", "rd", "am" o "pe" (para el color)
    public string EstadoClase { get; set; } = "";
}

public class ClienteDetalleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime ClienteDesde { get; set; }

    public int Visitas { get; set; }
    public DateTime? UltimaVisita { get; set; }
    public int TotalNotas { get; set; }

    public List<PreferenciaViewModel> Preferencias { get; set; } = new();
    public List<NotaClienteViewModel> Notas { get; set; } = new();
    public List<HistorialClienteItemViewModel> Historial { get; set; } = new();

    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;

    // Para volver a la búsqueda con el mismo texto
    public string? VolverQ { get; set; }
}