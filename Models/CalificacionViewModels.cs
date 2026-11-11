namespace DKaiza.Web.Models;

// ───────── HU-14 · Historial de citas ─────────

public class HistorialItemViewModel
{
    public int Id { get; set; }
    public DateTime Inicio { get; set; }   // hora local del salón
    public DateTime Fin { get; set; }
    public string Servicio { get; set; } = "";
    public int EstilistaId { get; set; }
    public string Estilista { get; set; } = "";
    public bool EstilistaTieneFoto { get; set; }
    public decimal Precio { get; set; }

    // "por" = completada sin calificar · "rat" = completada y calificada · "can" = cancelada
    public string Clase { get; set; } = "por";

    public int? Estrellas { get; set; }
    public string? Comentario { get; set; }
}

public class HistorialViewModel
{
    public List<HistorialItemViewModel> Items { get; set; } = new();

    public int Completadas => Items.Count(i => i.Clase != "can");
    public int PorCalificar => Items.Count(i => i.Clase == "por");
    public int Calificadas => Items.Count(i => i.Clase == "rat");
    public int Canceladas => Items.Count(i => i.Clase == "can");
}

// ───────── HU-14 · Calificar un servicio ─────────

public class CalificarViewModel
{
    public int CitaId { get; set; }
    public string Servicio { get; set; } = "";
    public DateTime Inicio { get; set; }   // hora local del salón
    public DateTime Fin { get; set; }
    public decimal Precio { get; set; }

    public int EstilistaId { get; set; }
    public string Estilista { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public bool EstilistaTieneFoto { get; set; }

    // Nombre con el que se mostrará el comentario, por ejemplo "Camila R."
    public string ClienteAbreviado { get; set; } = "";

    // Solo las citas Completadas se pueden calificar
    public bool Asistio { get; set; }

    // Datos del formulario (para volver a mostrarlos si hay un error)
    public int Estrellas { get; set; }
    public string? Comentario { get; set; }

    // Estado cuando ya fue calificada
    public bool YaCalificada { get; set; }
    public bool Gracias { get; set; }
}

// ───────── HU-14 · Estilistas y sus opiniones (vista del cliente) ─────────

public class EstilistaTarjetaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";                 // "Manicure · Coloración"
    public List<string> Especialidades { get; set; } = new();
    public bool TieneFoto { get; set; }

    public double? Promedio { get; set; }  // null = aún sin calificaciones
    public int Total { get; set; }

    // Comentario más reciente (con texto)
    public string? UltimoComentario { get; set; }
    public string? UltimoCliente { get; set; }
    public string? UltimoServicio { get; set; }
}

public class EstilistasViewModel
{
    public List<EstilistaTarjetaViewModel> Estilistas { get; set; } = new();
}

public class ComentarioViewModel
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";   // abreviado: "Camila R."
    public int Estrellas { get; set; }
    public string Comentario { get; set; } = "";
    public string Servicio { get; set; } = "";
    public DateTime Fecha { get; set; }         // hora local del salón
}

public class PerfilEstilistaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public List<string> Especialidades { get; set; } = new();
    public bool TieneFoto { get; set; }
    public TimeOnly InicioJornada { get; set; }
    public TimeOnly FinJornada { get; set; }

    public double? Promedio { get; set; }
    public int TotalCalificaciones { get; set; }

    // Índice 1..5 = cantidad de calificaciones con esas estrellas (el 0 no se usa)
    public int[] Distribucion { get; set; } = new int[6];

    public DateTime Hoy { get; set; }           // fecha de hoy en hora del salón
    public List<ComentarioViewModel> Comentarios { get; set; } = new();

    // Filtro por estrellas: null = todas, 5, 4 o 3 (3 significa "3 o menos")
    public int? Filtro { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalComentarios { get; set; }
}