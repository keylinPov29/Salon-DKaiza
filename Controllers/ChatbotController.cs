using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DKaiza.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

/// <summary>
/// API para el chatbot de atención al cliente.
/// Procesa consultas con análisis inteligente de lenguaje natural y consultas a la base de datos.
/// </summary>
[Route("api/chatbot")]
[ApiController]
[IgnoreAntiforgeryToken]
public class ChatbotController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatbotController(ApplicationDbContext db) => _db = db;

    // ── Preguntas sugeridas ────────────────────────────────────────────
    public record PreguntaSugerida(string Id, string Texto);

    [HttpGet("sugerencias")]
    public IActionResult ObtenerSugerencias()
    {
        var sugerencias = new List<PreguntaSugerida>
        {
            new("precios",    "💰 Precios y Servicios"),
            new("horarios",   "🕐 Horarios de atención"),
            new("estilistas", "💇‍♀️ Conoce a nuestras Estilistas"),
            new("ubicacion",  "📍 Ubicación del salón"),
            new("pagos",      "💳 Métodos de pago"),
            new("reserva",    "📅 ¿Cómo reservar una cita?")
        };
        return Ok(sugerencias);
    }

    // ── Responder consulta ─────────────────────────────────────────────
    public record ConsultaDto(string Mensaje);

    [HttpPost("consultar")]
    public async Task<IActionResult> Consultar([FromBody] ConsultaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensaje))
            return BadRequest(new { respuesta = "Por favor, escribe tu consulta." });

        var textoCrudo = dto.Mensaje.Trim();
        var textoNorm = NormalizarTexto(textoCrudo);

        string respuesta = await ResolverRespuesta(textoCrudo, textoNorm);
        return Ok(new { respuesta });
    }

    // ── Endpoint directo para precios y servicios ──────────────────────
    [HttpGet("servicios")]
    public async Task<IActionResult> ObtenerServicios()
    {
        var categorias = await _db.CategoriasServicio
            .Where(c => c.Activo)
            .Include(c => c.Servicios.Where(s => s.Activo))
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .Select(c => new
            {
                categoria = c.Nombre,
                servicios = c.Servicios
                    .OrderBy(s => s.Nombre)
                    .Select(s => new
                    {
                        nombre = s.Nombre,
                        precio = s.Precio,
                        duracion = s.DuracionMinutos
                    })
            })
            .ToListAsync();

        return Ok(categorias);
    }

    // ── Lógica de resolución inteligente ──────────────────────────────
    private async Task<string> ResolverRespuesta(string textoCrudo, string t)
    {
        // 1. Saludo cordial
        if (EsSaludo(t))
        {
            return "¡Hola! 👋 Te damos la bienvenida a <strong>Salón de Belleza D'Kaiza</strong>.<br><br>" +
                   "¿En qué puedo ayudarte hoy? Puedes consultarme sobre:<br>" +
                   "• Precios de servicios específicos (ej: <em>¿cuánto está el corte de cabello?</em>)<br>" +
                   "• Horarios y días de atención<br>" +
                   "• Nuestras estilistas profesionales<br>" +
                   "• Métodos de pago y reservas 😊";
        }

        // 2. Agradecimiento
        if (Contiene(t, "gracias", "muchas gracias", "mil gracias", "agradecido", "excelente", "perfecto", "genial"))
        {
            return "¡Ha sido un placer ayudarte! 😊 Si tienes alguna otra duda o consulta sobre nuestros servicios, aquí estaré. ¡Te esperamos pronto en <strong>D'Kaiza</strong>! ✨💇‍♀️";
        }

        // 3. Despedida
        if (Contiene(t, "adios", "chao", "bye", "hasta luego", "nos vemos", "buenas noches", "hasta pronto"))
        {
            return "¡Hasta pronto! 👋 Que tengas un excelente día. Recuerda que puedes agendar tu cita cuando gustes. ¡Te esperamos en <strong>D'Kaiza</strong>! 💖";
        }

        // 4. Búsqueda específica de servicios en Base de Datos
        // Ej: "cuánto está el corte de cabello", "precio de manicure", "tienen balayage", "cepillado"
        var serviciosEspecificos = await BuscarServiciosEnDb(t);
        if (serviciosEspecificos != null)
        {
            return serviciosEspecificos;
        }

        // 5. Catálogo general de Precios y Servicios
        if (Contiene(t, "precio", "precios", "costo", "costos", "tarifa", "tarifas", "cuanto esta", 
                        "cuanto cuesta", "cuanto sale", "cuanto cobran", "cuanto vale", "a cuanto", 
                        "catalogo", "servicios", "carta", "lista", "que ofrecen", "que tienen", "que hacen"))
        {
            return await GenerarRespuestaCatalogoCompleto();
        }

        // 6. Horarios de atención
        if (Contiene(t, "horario", "horarios", "hora", "horas", "abierto", "abren", "abrir", "cierra", 
                        "cierran", "cierre", "atencion", "atienden", "atender", "dias", "domingo", 
                        "sabado", "feriado", "feriados", "tarde", "temprano"))
        {
            return "🕐 <strong>Horario de atención en D'Kaiza:</strong><br><br>" +
                   "📅 <strong>Lunes a Sábado:</strong> 9:30 AM – 9:00 PM<br>" +
                   "🍽️ <strong>Refrigerio mediodía:</strong> 1:00 PM – 3:00 PM<br>" +
                   "🚫 <strong>Domingos:</strong> Cerrado (previa coordinación para eventos)<br><br>" +
                   "💡 <em>Te recomendamos agendar con anticipación para asegurar tu horario preferido.</em>";
        }

        // 7. Estilistas y profesionales
        if (Contiene(t, "estilista", "estilistas", "quien atiende", "quienes atienden", "profesional", 
                        "profesionales", "personal", "equipo", "especialista", "especialistas", 
                        "quien me atiende", "quienes trabajan"))
        {
            return await GenerarRespuestaEstilistas();
        }

        // 8. Ubicación y cómo llegar
        if (Contiene(t, "ubicacion", "donde", "direccion", "donde estan", "donde queda", "como llegar", 
                        "llegar", "mapa", "queda", "estan", "local", "salon", "sede", "distrito", 
                        "avenida", "calle", "referencia"))
        {
            return "📍 <strong>Ubicación de Salón D'Kaiza:</strong><br><br>" +
                   "🏢 <strong>Dirección:</strong> Av. Principal 123, Lima, Perú.<br>" +
                   "🚗 <strong>Referencia:</strong> A media cuadra del parque central, zona segura y de fácil acceso.<br>" +
                   "🗺️ ¡Te esperamos para brindarte la mejor experiencia de belleza!";
        }

        // 9. Cómo reservar / Citas
        if (Contiene(t, "reserva", "reservar", "cita", "citas", "agendar", "agenda", "turno", 
                        "separar", "apartar", "programar", "como pido cita", "como reservo", "sacar cita"))
        {
            return "📅 <strong>¿Cómo reservar tu cita en D'Kaiza?</strong><br><br>" +
                   "1️⃣ <strong>Inicia sesión</strong> o regístrate en nuestra web.<br>" +
                   "2️⃣ Ingresa a la sección <strong>Servicios</strong>.<br>" +
                   "3️⃣ Elige el servicio deseado y selecciona a tu <strong>estilista favorita</strong>.<br>" +
                   "4️⃣ Escoge la <strong>fecha y hora</strong> que mejor te convenga.<br>" +
                   "5️⃣ Confirma tu reserva y listo 🎉<br><br>" +
                   "📲 Recibirás confirmación y recordatorio automático de tu cita.";
        }

        // 10. Métodos de pago
        if (Contiene(t, "pago", "pagos", "pagar", "metodo", "metodos", "forma de pago", "formas de pago", 
                        "tarjeta", "tarjetas", "efectivo", "yape", "plin", "transferencia", "visa", 
                        "mastercard", "debito", "credito", "pos"))
        {
            return "💳 <strong>Métodos de pago aceptados:</strong><br><br>" +
                   "• 💵 <strong>Efectivo</strong> en moneda nacional (Soles)<br>" +
                   "• 📱 <strong>Billeteras digitales:</strong> Yape y Plin<br>" +
                   "• 💳 <strong>Tarjetas:</strong> Visa, Mastercard, American Express (Débito y Crédito)<br>" +
                   "• 🏦 <strong>Transferencias bancarias</strong> inmediatas (BCP, BBVA, Interbank)<br><br>" +
                   "¡Puedes pagar cómodamente en caja o al momento de confirmar tu reserva!";
        }

        // 11. Contacto directo
        if (Contiene(t, "contacto", "telefono", "celular", "llamar", "whatsapp", "correo", 
                        "email", "comunicar", "redes", "instagram", "facebook", "tiktok"))
        {
            return "📞 <strong>Canales de atención directa:</strong><br><br>" +
                   "📱 <strong>Teléfono / WhatsApp:</strong> +51 999 888 777<br>" +
                   "✉️ <strong>Correo electrónico:</strong> hola@dkaiza.com<br>" +
                   "📸 <strong>Instagram:</strong> @@dkaiza_salon<br>" +
                   "📍 <strong>Dirección:</strong> Av. Principal 123, Lima<br><br>" +
                   "¡Estamos a tu disposición para cualquier consulta adicional!";
        }

        // 12. Promociones / Descuentos
        if (Contiene(t, "promocion", "promociones", "descuento", "descuentos", "oferta", "ofertas", 
                        "paquete", "paquetes", "combo", "combos", "rebaja"))
        {
            return "✨ <strong>Promociones y Beneficios D'Kaiza:</strong><br><br>" +
                   "🎁 <strong>Clientes Frecuentes:</strong> Acumula visitas y accede a tarifas preferenciales VIP.<br>" +
                   "💇‍♀️ <strong>Combos especiales:</strong> Pregunta por nuestros paquetes de Corte + Tratamiento + Cepillado.<br>" +
                   "🎂 <strong>Cumpleaños:</strong> ¡Descuento especial del 15% durante la semana de tu cumpleaños!<br><br>" +
                   "Pregunta en recepción por las promociones vigentes de este mes.";
        }

        // 13. Respuesta guiada por defecto con ejemplos prácticos
        return "🤔 No logré comprender del todo tu consulta. Pero puedo responderte sobre cualquiera de estos temas:<br><br>" +
               "💬 <strong>Prueba preguntándome:</strong><br>" +
               "• <em>«¿Cuánto está el corte de cabello?»</em><br>" +
               "• <em>«¿Cuánto cuesta el tinte o manicure?»</em><br>" +
               "• <em>«¿Cuáles son los horarios de atención?»</em><br>" +
               "• <em>«¿Quiénes son las estilistas?»</em><br>" +
               "• <em>«¿Aceptan Yape o tarjeta?»</em><br>" +
               "• <em>«¿Dónde están ubicados?»</em><br><br>" +
               "O presiona los botones sugeridos en la parte inferior 👇";
    }

    // ── Búsqueda inteligente de servicios específicos en BD ──────────
    private async Task<string?> BuscarServiciosEnDb(string textoNormalizado)
    {
        // Obtenemos todos los servicios activos
        var servicios = await _db.Servicios
            .Where(s => s.Activo)
            .Include(s => s.Categoria)
            .AsNoTracking()
            .ToListAsync();

        if (!servicios.Any()) return null;

        // Palabras no relevantes para la búsqueda de servicio
        var palabrasIgnoradas = new HashSet<string>
        {
            "cuanto", "esta", "cuesta", "sale", "vale", "precio", "costo", "tarifa", "de", "el", "la", "los", "las", 
            "un", "una", "por", "favor", "en", "para", "del", "al", "que", "hacen", "tienen", "quisiera", "saber"
        };

        var palabrasConsulta = textoNormalizado
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= 3 && !palabrasIgnoradas.Contains(p))
            .ToList();

        if (!palabrasConsulta.Any()) return null;

        // Buscamos servicios que coincidan con las palabras clave (nombre o categoría o descripción)
        var coincidencias = new List<Web.Dominio.Servicio>();

        foreach (var s in servicios)
        {
            var nombreNorm = NormalizarTexto(s.Nombre);
            var catNorm = s.Categoria != null ? NormalizarTexto(s.Categoria.Nombre) : "";
            var descNorm = NormalizarTexto(s.Descripcion ?? "");

            bool coincide = palabrasConsulta.Any(p => 
                nombreNorm.Contains(p) || catNorm.Contains(p) || descNorm.Contains(p));

            if (coincide)
            {
                coincidencias.Add(s);
            }
        }

        if (!coincidencias.Any()) return null;

        // Generamos la respuesta con los servicios encontrados
        var sb = new StringBuilder();
        sb.Append("✨ <strong>Encontré los siguientes servicios para ti:</strong><br><br>");

        foreach (var serv in coincidencias.Take(5))
        {
            var catNombre = serv.Categoria?.Nombre ?? "Servicio";
            sb.Append($"💇 <strong>{serv.Nombre}</strong> ({catNombre})<br>");
            sb.Append($"&nbsp;&nbsp;💵 <strong>Precio: S/ {serv.Precio:F2}</strong><br>");
            sb.Append($"&nbsp;&nbsp;⏱️ <em>Duración estimada: {serv.DuracionMinutos} minutos</em><br>");
            if (!string.IsNullOrWhiteSpace(serv.Descripcion))
            {
                sb.Append($"&nbsp;&nbsp;📝 <small>{serv.Descripcion}</small><br>");
            }
            sb.Append("<br>");
        }

        sb.Append("📅 ¿Deseas agendarlo? ¡Inicia sesión y selecciona tu servicio en la sección de reservas! 💖");
        return sb.ToString();
    }

    // ── Catálogo completo de servicios agrupado ───────────────────────
    private async Task<string> GenerarRespuestaCatalogoCompleto()
    {
        var categorias = await _db.CategoriasServicio
            .Where(c => c.Activo)
            .Include(c => c.Servicios.Where(s => s.Activo))
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();

        if (!categorias.Any())
            return "Actualmente estamos actualizando nuestro catálogo. ¡Vuelve a consultar en unos momentos! 🔄";

        var sb = new StringBuilder();
        sb.Append("💰 <strong>Catálogo y tarifas de Salón D'Kaiza:</strong><br><br>");

        foreach (var cat in categorias)
        {
            if (!cat.Servicios.Any()) continue;

            sb.Append($"<strong>✨ {cat.Nombre}</strong><br>");
            foreach (var serv in cat.Servicios.OrderBy(s => s.Nombre))
            {
                sb.Append($"&nbsp;&nbsp;• {serv.Nombre} — <strong>S/ {serv.Precio:F2}</strong> <small>({serv.DuracionMinutos} min)</small><br>");
            }
            sb.Append("<br>");
        }

        sb.Append("📅 <em>¿Deseas reservar alguno? ¡Pregúntame cómo o elige la opción en el menú!</em>");
        return sb.ToString();
    }

    // ── Lista de estilistas desde la BD ───────────────────────────────
    private async Task<string> GenerarRespuestaEstilistas()
    {
        var estilistas = await _db.Estilistas
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombres)
            .AsNoTracking()
            .ToListAsync();

        if (!estilistas.Any())
            return "Nuestras estilistas están registradas en el sistema. Pronto podrás conocer más detalles de cada una.";

        var sb = new StringBuilder();
        sb.Append("💇‍♀️ <strong>Nuestras estilistas profesionales en D'Kaiza:</strong><br><br>");

        foreach (var est in estilistas)
        {
            sb.Append($"👩‍🦰 <strong>{est.NombreCompleto}</strong><br>");
            sb.Append($"&nbsp;&nbsp;🌟 Especialidad: <em>{est.Especialidad}</em><br>");
            sb.Append($"&nbsp;&nbsp;🕐 Jornada: {est.InicioJornada:hh\\:mm} a {est.FinJornada:hh\\:mm}<br><br>");
        }

        sb.Append("📅 Puedes seleccionar a tu estilista de preferencia al momento de solicitar tu cita.");
        return sb.ToString();
    }

    // ── Utilidades de análisis y normalización ────────────────────────
    private static bool EsSaludo(string t)
    {
        var saludos = new[] { "hola", "buenas", "buenos dias", "buenas tardes", "buenas noches", "hey", "saludos", "que tal" };
        return saludos.Any(s => t == s || t.StartsWith(s + " ") || t.EndsWith(" " + s));
    }

    private static bool Contiene(string texto, params string[] terminos)
        => terminos.Any(t => texto.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";

        var lower = texto.ToLowerInvariant().Trim();

        // Eliminar diacríticos (tildes, diéresis)
        var normalizedString = lower.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var sinTildes = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Reemplazar signos de puntuación por espacios
        var sinPuntuacion = Regex.Replace(sinTildes, @"[¿?¡!.,;:_\-/\(\)""']", " ");
        // Reducir espacios múltiples
        return Regex.Replace(sinPuntuacion, @"\s+", " ").Trim();
    }
}
