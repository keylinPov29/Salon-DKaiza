using System.Security.Claims;
using System.Text;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using DKaiza.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

/// <summary>
/// Controlador para la visualización, descarga e impresión del comprobante digital de venta (ticket).
/// </summary>
[Authorize]
[Route("comprobante")]
[Route("comprobantes")]
public class ComprobantesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IComprobanteService _comprobanteService;

    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    public ComprobantesController(ApplicationDbContext db, IComprobanteService comprobanteService)
    {
        _db = db;
        _comprobanteService = comprobanteService;
    }

    // ── GET /comprobante/{id} o /comprobante/cita/{id} ──────────────────────────────────────────
    [HttpGet("{id:int}")]
    [HttpGet("cita/{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var cita = await ObtenerCitaConDetalles(id);
        if (cita == null) return NotFound("La cita no existe.");

        if (!TienePermisoParaVer(cita)) return Forbid();

        // El comprobante solo existe si el pago fue registrado correctamente
        if (cita.Estado != EstadoCita.Pagada && cita.FechaPago == null)
        {
            ViewBag.CitaId = id;
            ViewBag.Mensaje = "El comprobante de venta digital aún no está disponible porque no se ha registrado un pago para esta cita.";
            return View("NoDisponible");
        }

        // Asegurar que el comprobante esté generado y registrado en BD de forma automática y trazable
        var comprobantePersistido = await _comprobanteService.GenerarComprobanteAsync(cita.Id);

        var model = MapearComprobante(cita);
        if (comprobantePersistido != null)
        {
            model.Id = comprobantePersistido.Id;
            model.NumeroComprobante = comprobantePersistido.NumeroComprobante;
        }

        return View("Detalle", model);
    }

    // ── GET /comprobante/{id}/descargar ────────────────────────────────
    [HttpGet("{id:int}/descargar")]
    [HttpGet("cita/{id:int}/descargar")]
    public async Task<IActionResult> Descargar(int id)
    {
        var cita = await ObtenerCitaConDetalles(id);
        if (cita == null) return NotFound();

        if (!TienePermisoParaVer(cita)) return Forbid();

        if (cita.Estado != EstadoCita.Pagada && cita.FechaPago == null)
        {
            return BadRequest("El pago no ha sido registrado.");
        }

        var comprobantePersistido = await _comprobanteService.GenerarComprobanteAsync(cita.Id);
        var model = MapearComprobante(cita);
        if (comprobantePersistido != null)
        {
            model.Id = comprobantePersistido.Id;
            model.NumeroComprobante = comprobantePersistido.NumeroComprobante;
        }

        var html = GenerarHtmlDescargable(model);
        var bytes = Encoding.UTF8.GetBytes(html);

        var nombreArchivo = $"Comprobante_{model.NumeroComprobante}.html";
        return File(bytes, "text/html; charset=utf-8", nombreArchivo);
    }

    // ── Métodos auxiliares ─────────────────────────────────────────────
    private async Task<Cita?> ObtenerCitaConDetalles(int id)
    {
        return await _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
                .ThenInclude(s => s!.Categoria)
            .Include(c => c.Estilista)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    private bool TienePermisoParaVer(Cita cita)
    {
        if (User.IsInRole("Administrador") || User.IsInRole("Recepcionista"))
            return true;

        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return cita.ClienteId == usuarioId;
        }

        return false;
    }

    private ComprobanteViewModel MapearComprobante(Cita cita)
    {
        var fechaPagoUtc = cita.FechaPago ?? cita.FechaReserva;
        var fechaPagoLocal = TimeZoneInfo.ConvertTimeFromUtc(fechaPagoUtc, ZonaSalon);
        var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(cita.Inicio, ZonaSalon);
        var finLocal = TimeZoneInfo.ConvertTimeFromUtc(cita.Fin, ZonaSalon);

        var metodoTexto = cita.MetodoPago switch
        {
            MetodoPagoCita.Tarjeta => "Tarjeta de Crédito / Débito (Mercado Pago)",
            MetodoPagoCita.Efectivo => "Efectivo",
            _ => "Pago Digital"
        };

        return new ComprobanteViewModel
        {
            CitaId = cita.Id,
            NumeroOperacion = cita.NumeroOperacion ?? $"OP-{cita.Id:D6}",
            FechaEmision = fechaPagoLocal,
            FechaCitaInicio = inicioLocal,
            FechaCitaFin = finLocal,
            ClienteId = cita.ClienteId,
            ClienteNombre = cita.Cliente?.NombreCompleto ?? "Cliente D'Kaiza",
            ClienteEmail = cita.Cliente?.Email ?? "",
            ClienteTelefono = cita.Cliente?.Telefono ?? "",
            ServicioId = cita.ServicioId,
            ServicioNombre = cita.Servicio?.Nombre ?? "Servicio de Belleza",
            CategoriaNombre = cita.Servicio?.Categoria?.Nombre ?? "Salón",
            DuracionMinutos = cita.Servicio?.DuracionMinutos ?? (int)(cita.Fin - cita.Inicio).TotalMinutes,
            EstilistaNombre = cita.Estilista?.NombreCompleto ?? "Estilista D'Kaiza",
            PrecioBase = cita.PrecioBase > 0 ? cita.PrecioBase : cita.PrecioTotal + cita.Descuento,
            Descuento = cita.Descuento,
            CuponCodigo = cita.CuponCodigo,
            TotalPagado = cita.PrecioTotal,
            MetodoPago = metodoTexto,
            EstadoPago = "PAGADO"
        };
    }

    private static string GenerarHtmlDescargable(ComprobanteViewModel m)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Comprobante {m.NumeroComprobante} - D'Kaiza</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; }}
  body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #f6f3ee; color: #3a2e2b; padding: 20px; }}
  .ticket {{ max-width: 520px; margin: 0 auto; background: #fff; border: 1px solid #e0d7cd; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,0.08); overflow: hidden; }}
  .header {{ background: #7e675a; color: #fff; text-align: center; padding: 24px 20px; }}
  .header h1 {{ font-size: 22px; font-weight: 700; letter-spacing: 1px; margin-bottom: 4px; }}
  .header p {{ font-size: 11px; opacity: 0.9; margin: 2px 0; }}
  .badge {{ display: inline-block; background: #557A58; color: #fff; font-size: 11px; font-weight: 700; text-transform: uppercase; padding: 4px 14px; border-radius: 20px; margin-top: 10px; }}
  .body {{ padding: 24px; }}
  .num-box {{ text-align: center; border: 2px dashed #7e675a; background: #faf8f5; border-radius: 8px; padding: 12px; margin-bottom: 20px; }}
  .num-box .tipo {{ font-size: 11px; text-transform: uppercase; color: #7e675a; font-weight: 700; letter-spacing: 1.5px; }}
  .num-box .num {{ font-size: 20px; font-weight: 800; color: #4a3730; margin-top: 2px; }}
  .num-box .op {{ font-size: 11px; color: #8a7568; margin-top: 4px; }}
  .section-title {{ font-size: 12px; font-weight: 700; text-transform: uppercase; color: #7e675a; letter-spacing: 1px; margin-top: 16px; margin-bottom: 8px; border-bottom: 1px solid #eee; padding-bottom: 4px; }}
  .row {{ display: flex; justify-content: space-between; font-size: 13px; padding: 5px 0; }}
  .row .lbl {{ color: #7a6b65; }}
  .row .val {{ font-weight: 600; color: #2e2421; text-align: right; }}
  .total-row {{ display: flex; justify-content: space-between; font-size: 16px; font-weight: 800; color: #477765; border-top: 2px solid #7e675a; margin-top: 12px; padding-top: 10px; }}
  .footer {{ background: #faf8f5; text-align: center; padding: 16px; font-size: 11px; color: #8a7568; border-top: 1px dashed #e0d7cd; }}
  .btn-print {{ display: block; width: 100%; max-width: 520px; margin: 15px auto 0; padding: 12px; background: #7e675a; color: #fff; text-align: center; text-decoration: none; border-radius: 8px; font-weight: 600; border: none; cursor: pointer; }}
  @media print {{
    body {{ background: #fff; padding: 0; }}
    .ticket {{ box-shadow: none; border: none; max-width: 100%; }}
    .btn-print {{ display: none; }}
  }}
</style>
</head>
<body>
  <div class=""ticket"">
    <div class=""header"">
      <h1>D'KAIZA</h1>
      <p>{m.EmpresaNombre}</p>
      <p>RUC: {m.EmpresaRuc}</p>
      <p>{m.EmpresaDireccion}</p>
      <p>Tel: {m.EmpresaTelefono} | {m.EmpresaEmail}</p>
      <span class=""badge"">✓ {m.EstadoPago}</span>
    </div>
    <div class=""body"">
      <div class=""num-box"">
        <div class=""tipo"">BOLETA DE VENTA ELECTRÓNICA</div>
        <div class=""num"">{m.NumeroComprobante}</div>
        <div class=""op"">N° Operación: {m.NumeroOperacion} · Emisión: {m.FechaEmision:dd/MM/yyyy HH:mm}</div>
      </div>

      <div class=""section-title"">Datos del Cliente</div>
      <div class=""row""><span class=""lbl"">Cliente:</span><span class=""val"">{m.ClienteNombre}</span></div>
      <div class=""row""><span class=""lbl"">Email:</span><span class=""val"">{m.ClienteEmail}</span></div>
      <div class=""row""><span class=""lbl"">Teléfono:</span><span class=""val"">{m.ClienteTelefono}</span></div>

      <div class=""section-title"">Detalle del Servicio</div>
      <div class=""row""><span class=""lbl"">Servicio:</span><span class=""val"">{m.ServicioNombre}</span></div>
      <div class=""row""><span class=""lbl"">Categoría:</span><span class=""val"">{m.CategoriaNombre}</span></div>
      <div class=""row""><span class=""lbl"">Estilista:</span><span class=""val"">{m.EstilistaNombre}</span></div>
      <div class=""row""><span class=""lbl"">Fecha y Hora Cita:</span><span class=""val"">{m.FechaCitaInicio:dd/MM/yyyy} ({m.FechaCitaInicio:hh\:mm tt} – {m.FechaCitaFin:hh\:mm tt})</span></div>
      <div class=""row""><span class=""lbl"">Duración:</span><span class=""val"">{m.DuracionMinutos} min</span></div>

      <div class=""section-title"">Liquidación</div>
      <div class=""row""><span class=""lbl"">Precio base:</span><span class=""val"">S/ {m.PrecioBase:F2}</span></div>
      {(m.Descuento > 0 ? $@"<div class=""row""><span class=""lbl"">Descuento ({m.CuponCodigo}):</span><span class=""val"" style=""color:#b83232;"">− S/ {m.Descuento:F2}</span></div>" : "")}
      <div class=""row""><span class=""lbl"">Op. Gravada:</span><span class=""val"">S/ {m.SubtotalGravado:F2}</span></div>
      <div class=""row""><span class=""lbl"">I.G.V. (18%):</span><span class=""val"">S/ {m.MontoIgv:F2}</span></div>
      <div class=""total-row""><span>TOTAL PAGADO</span><span>S/ {m.TotalPagado:F2}</span></div>
      <div class=""row"" style=""margin-top:8px;""><span class=""lbl"">Método de pago:</span><span class=""val"">{m.MetodoPago}</span></div>
    </div>
    <div class=""footer"">
      <p>Gracias por confiar en <strong>D'Kaiza</strong>. ¡Elegancia que transforma!</p>
      <p style=""margin-top:4px; font-size:10px; color:#aaa;"">Código de Trazabilidad: {m.CodigoVerificacion}</p>
    </div>
  </div>
  <button class=""btn-print"" onclick=""window.print()"">🖨️ Imprimir / Guardar como PDF</button>
</body>
</html>";
    }
}
