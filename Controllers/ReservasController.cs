using System.Security.Claims;
using System.Text.Json;
using DKaiza.Data;
using DKaiza.Web.Dominio;
using DKaiza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Controllers;

/// <summary>
/// Flujo de reserva de cita para clientes autenticados.
/// GET  /reservas/nueva?servicioId=N  →  Paso 1: calendario
/// GET  /reservas/estilistas?servicioId=N&fecha=YYYY-MM-DD  →  Paso 2: estilistas
/// GET  /reservas/horarios?servicioId=N&fecha=YYYY-MM-DD&estilistaId=N  →  Paso 3: horas
/// POST /reservas/confirmar  →  crea la cita (atómico) → resumen
/// GET  /reservas/resumen/{id}  →  resumen final / pago
/// </summary>
[Route("reservas")]
[Authorize(Roles = "Cliente")]
public class ReservasController : Controller
{
    private const int MesesCalendarioReserva = 3;
    private const string CodigoCuponValido = "DKAIZA10";
    private const decimal PorcentajeDescuentoCupon = 0.10m;
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReservasController> _logger;

    // Zona horaria del salón (Perú = UTC-5, sin cambio de horario)
    private static readonly TimeZoneInfo ZonaSalon =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    public ReservasController(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ReservasController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  PASO 1 — Calendario (mes actual y los dos siguientes)
    // ────────────────────────────────────────────────────────────────
    [HttpGet("nueva")]
    public async Task<IActionResult> PasoUno(int servicioId)
    {
        var servicio = await _db.Servicios
            .AsNoTracking()
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

        if (servicio == null)
        {
            TempData["Error"] = "El servicio seleccionado no existe o no está disponible.";
            return Redirect("/");
        }

        var hoyLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaSalon));
        var hasta = FinVentanaReserva(hoyLocal);
        var inicioRangoUtc = AFechaUtc(hoyLocal, TimeOnly.MinValue);
        var finRangoUtc = AFechaUtc(hasta.AddDays(1), TimeOnly.MinValue);

        // Estilistas activos y sus horarios
        var estilistas = await _db.Estilistas
            .AsNoTracking()
            .Where(e => e.Activo)
            .Select(e => new
            {
                e.Id,
                e.Especialidad,
                e.InicioJornada,
                e.FinJornada,
                e.InicioDescanso,
                e.FinDescanso
            })
            .ToListAsync();
        estilistas = estilistas
            .Where(e => CoincideEspecialidad(e.Especialidad, servicio.Categoria?.Nombre))
            .ToList();

        // Citas ya reservadas en el rango (no canceladas)
        var citasExistentes = await _db.Citas
            .AsNoTracking()
            .Where(c => c.Estado != EstadoCita.Cancelada
                     && c.Inicio >= inicioRangoUtc
                     && c.Inicio < finRangoUtc)
            .Select(c => new { c.EstilistaId, c.Inicio, c.Fin })
            .ToListAsync();

        var diasDisponibles = new List<DateOnly>();

        for (var dia = hoyLocal.AddDays(1); dia <= hasta; dia = dia.AddDays(1))
        {
            // Salón abre Lunes–Sábado (DayOfWeek: 1-6)
            var dow = dia.DayOfWeek;
            if (dow == DayOfWeek.Sunday) continue;

            // Verificar si hay al menos un slot libre en algún estilista
            bool haySlot = false;
            foreach (var e in estilistas)
            {
                var slotsOcupados = citasExistentes
                    .Where(c => c.EstilistaId == e.Id
                             && DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(c.Inicio, ZonaSalon)) == dia)
                    .ToList();

                var slots = GenerarSlots(dia, e.InicioJornada, e.FinJornada,
                                         e.InicioDescanso, e.FinDescanso,
                                         servicio.DuracionMinutos);

                foreach (var slot in slots)
                {
                    var slotUtcInicio = TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(slot), ZonaSalon);
                    var slotUtcFin   = slotUtcInicio.AddMinutes(servicio.DuracionMinutos);

                    bool ocupado = slotsOcupados.Any(c =>
                        slotUtcInicio < c.Fin && slotUtcFin > c.Inicio);

                    if (!ocupado) { haySlot = true; break; }
                }
                if (haySlot) break;
            }

            if (haySlot) diasDisponibles.Add(dia);
        }

        var vm = new ReservaPasoUnoViewModel
        {
            ServicioId      = servicio.Id,
            ServicioNombre  = servicio.Nombre,
            DuracionMinutos = servicio.DuracionMinutos,
            Precio          = servicio.Precio,
            DiasDisponibles = diasDisponibles,
            FechaMinima = hoyLocal,
            FechaMaxima = hasta
        };

        return View("PasoUno", vm);
    }

    // ────────────────────────────────────────────────────────────────
    //  PASO 2 — Estilistas disponibles ese día
    // ────────────────────────────────────────────────────────────────
    [HttpGet("estilistas")]
    public async Task<IActionResult> PasoDos(int servicioId, string fecha)
    {
        if (!DateOnly.TryParse(fecha, out var fechaElegida) || !EsFechaReservaValida(fechaElegida))
        {
            TempData["Error"] = "La fecha seleccionada ya no está disponible.";
            return RedirectToAction(nameof(PasoUno), new { servicioId });
        }

        var servicio = await _db.Servicios
            .AsNoTracking()
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

        if (servicio == null) return Redirect("/");

        var estilistas = await _db.Estilistas
            .AsNoTracking()
            .Where(e => e.Activo)
            .Select(e => new
            {
                e.Id, e.Nombres, e.Apellidos, e.Especialidad,
                TieneFoto = e.Foto != null,
                e.InicioJornada, e.FinJornada, e.InicioDescanso, e.FinDescanso
            })
            .ToListAsync();
        estilistas = estilistas
            .Where(e => CoincideEspecialidad(e.Especialidad, servicio.Categoria?.Nombre))
            .ToList();

        // Citas del día para calcular slots libres por estilista
        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(fechaElegida.ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var finDiaUtc    = inicioDiaUtc.AddDays(1);

        var citasDelDia = await _db.Citas
            .AsNoTracking()
            .Where(c => c.Estado != EstadoCita.Cancelada
                     && c.Inicio >= inicioDiaUtc && c.Inicio < finDiaUtc)
            .Select(c => new { c.EstilistaId, c.Inicio, c.Fin })
            .ToListAsync();

        var lista = new List<EstilistaDisponibleDto>();

        foreach (var e in estilistas)
        {
            var slots = GenerarSlots(fechaElegida, e.InicioJornada, e.FinJornada,
                                     e.InicioDescanso, e.FinDescanso,
                                     servicio.DuracionMinutos);

            var ocupados = citasDelDia.Where(c => c.EstilistaId == e.Id).ToList();

            int libres = slots.Count(slot =>
            {
                var utcI = TimeZoneInfo.ConvertTimeToUtc(fechaElegida.ToDateTime(slot), ZonaSalon);
                var utcF = utcI.AddMinutes(servicio.DuracionMinutos);
                return !ocupados.Any(c => utcI < c.Fin && utcF > c.Inicio);
            });

            if (libres > 0)
            {
                lista.Add(new EstilistaDisponibleDto
                {
                    Id             = e.Id,
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}".Trim(),
                    Especialidad   = e.Especialidad,
                    TieneFoto      = e.TieneFoto,
                    HorariosLibres = libres
                });
            }
        }

        var vm = new ReservaPasoDosViewModel
        {
            ServicioId      = servicio.Id,
            ServicioNombre  = servicio.Nombre,
            DuracionMinutos = servicio.DuracionMinutos,
            Precio          = servicio.Precio,
            Fecha           = fechaElegida,
            Estilistas      = lista
        };

        return View("PasoDos", vm);
    }

    // ────────────────────────────────────────────────────────────────
    //  PASO 3 — Horarios disponibles
    // ────────────────────────────────────────────────────────────────
    [HttpGet("horarios")]
    public async Task<IActionResult> PasoTres(int servicioId, string fecha, int estilistaId)
    {
        if (!DateOnly.TryParse(fecha, out var fechaElegida) || !EsFechaReservaValida(fechaElegida))
        {
            TempData["Error"] = "La fecha seleccionada ya no está disponible.";
            return RedirectToAction(nameof(PasoUno), new { servicioId });
        }

        var servicio = await _db.Servicios
            .AsNoTracking()
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

        var estilista = await _db.Estilistas
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == estilistaId && e.Activo);

        if (servicio == null || estilista == null) return Redirect("/");
        if (!CoincideEspecialidad(estilista.Especialidad, servicio.Categoria?.Nombre))
        {
            TempData["Error"] = "Ese estilista no realiza el servicio seleccionado.";
            return RedirectToAction(nameof(PasoDos), new { servicioId, fecha });
        }

        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(fechaElegida.ToDateTime(TimeOnly.MinValue), ZonaSalon);
        var finDiaUtc    = inicioDiaUtc.AddDays(1);

        var citasDelDia = await _db.Citas
            .AsNoTracking()
            .Where(c => c.EstilistaId == estilistaId
                     && c.Estado != EstadoCita.Cancelada
                     && c.Inicio >= inicioDiaUtc && c.Inicio < finDiaUtc)
            .Select(c => new { c.Inicio, c.Fin })
            .ToListAsync();

        var todosSlots = GenerarSlots(fechaElegida, estilista.InicioJornada, estilista.FinJornada,
                                      estilista.InicioDescanso, estilista.FinDescanso,
                                      servicio.DuracionMinutos);

        var libres = todosSlots.Where(slot =>
        {
            var utcI = TimeZoneInfo.ConvertTimeToUtc(fechaElegida.ToDateTime(slot), ZonaSalon);
            var utcF = utcI.AddMinutes(servicio.DuracionMinutos);
            return !citasDelDia.Any(c => utcI < c.Fin && utcF > c.Inicio);
        }).ToList();

        var vm = new ReservaPasoTresViewModel
        {
            ServicioId            = servicio.Id,
            ServicioNombre        = servicio.Nombre,
            DuracionMinutos       = servicio.DuracionMinutos,
            Precio                = servicio.Precio,
            Fecha                 = fechaElegida,
            EstilistaId           = estilista.Id,
            EstilistaNombre       = estilista.NombreCompleto,
            EstilistaEspecialidad = estilista.Especialidad,
            HorariosDisponibles   = libres
        };

        return View("PasoTres", vm);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST CONFIRMAR — Crea la cita con bloqueo atómico
    // ────────────────────────────────────────────────────────────────
    [HttpPost("confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(int servicioId, string fecha, int estilistaId, string hora)
    {
        if (!DateOnly.TryParse(fecha, out var fechaElegida) || !EsFechaReservaValida(fechaElegida) ||
            !TimeOnly.TryParse(hora, out var horaElegida))
        {
            TempData["Error"] = "Datos de reserva inválidos. Por favor vuelve a intentarlo.";
            return Redirect("/");
        }

        var clienteIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(clienteIdStr, out var clienteId))
            return Forbid();

        var servicio  = await _db.Servicios.AsNoTracking()
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);
        var estilista = await _db.Estilistas.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == estilistaId && e.Activo);

        if (servicio == null || estilista == null)
        {
            TempData["Error"] = "El servicio o estilista no están disponibles.";
            return Redirect("/");
        }

        if (!CoincideEspecialidad(estilista.Especialidad, servicio.Categoria?.Nombre))
        {
            TempData["Error"] = "Ese estilista no realiza el servicio seleccionado.";
            return RedirectToAction(nameof(PasoDos), new { servicioId, fecha });
        }

        var horariosPermitidos = GenerarSlots(
            fechaElegida,
            estilista.InicioJornada,
            estilista.FinJornada,
            estilista.InicioDescanso,
            estilista.FinDescanso,
            servicio.DuracionMinutos);

        if (!horariosPermitidos.Contains(horaElegida))
        {
            TempData["Error"] = "El horario seleccionado ya no está disponible.";
            return RedirectToAction("PasoTres", new { servicioId, fecha, estilistaId });
        }

        // Convertir hora local del salón → UTC
        var inicioLocal = fechaElegida.ToDateTime(horaElegida);
        var inicioUtc   = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, ZonaSalon);
        var finUtc      = inicioUtc.AddMinutes(servicio.DuracionMinutos);

        // ── BLOQUEO ATÓMICO ──────────────────────────────────────────
        // Usamos una transacción serializable para evitar doble reserva.
        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        try
        {
            // Verifica si el slot sigue libre (dentro de la transacción)
            bool conflicto = await _db.Citas
                .AnyAsync(c => c.EstilistaId == estilistaId
                            && c.Estado != EstadoCita.Cancelada
                            && c.Inicio < finUtc
                            && c.Fin > inicioUtc);

            if (conflicto)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "¡Lo sentimos! Ese horario acaba de ser reservado por otra persona. Por favor elige otro.";
                return RedirectToAction("PasoTres", new { servicioId, fecha, estilistaId });
            }

            var cita = new Cita
            {
                ClienteId    = clienteId,
                ServicioId   = servicio.Id,
                EstilistaId  = estilista.Id,
                Inicio       = inicioUtc,
                Fin          = finUtc,
                PrecioTotal  = servicio.Precio,
                PrecioBase   = servicio.Precio,
                Estado       = EstadoCita.Pendiente,
                FechaReserva = DateTime.UtcNow
            };

            _db.Citas.Add(cita);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "¡Cita reservada con éxito!";
            return RedirectToAction("Resumen", new { id = cita.Id });
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Ocurrió un error al confirmar tu reserva. Por favor intenta de nuevo.";
            return RedirectToAction("PasoTres", new { servicioId, fecha, estilistaId });
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  RESUMEN — Confirmación + pago
    // ────────────────────────────────────────────────────────────────
    [HttpGet("resumen/{id:int}")]
    public async Task<IActionResult> Resumen(int id)
    {
        var clienteIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(clienteIdStr, out var clienteId))
            return Forbid();

        var cita = await _db.Citas
            .AsNoTracking()
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .FirstOrDefaultAsync(c => c.Id == id && c.ClienteId == clienteId);

        if (cita == null) return NotFound();

        var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(cita.Inicio, ZonaSalon);
        var finLocal    = TimeZoneInfo.ConvertTimeFromUtc(cita.Fin,    ZonaSalon);

        var vm = new ReservaResumenViewModel
        {
            CitaId         = cita.Id,
            ServicioNombre = cita.Servicio!.Nombre,
            EstilistaNombre = cita.Estilista!.NombreCompleto,
            Inicio         = inicioLocal,
            Fin            = finLocal,
            PrecioBase     = cita.PrecioBase,
            Descuento      = cita.Descuento,
            PrecioTotal    = cita.PrecioTotal,
            CuponCodigo    = cita.CuponCodigo,
            MetodoPago     = cita.MetodoPago?.ToString(),
            NumeroOperacion = cita.NumeroOperacion,
            Estado         = cita.Estado switch
            {
                EstadoCita.Pagada => "Pagada",
                EstadoCita.PendientePago => "Pendiente de pago",
                EstadoCita.Pendiente => "Pendiente",
                EstadoCita.Confirmada => "Confirmada",
                EstadoCita.Cancelada => "Cancelada",
                EstadoCita.EnProceso => "En proceso",
                EstadoCita.Completada => "Completada",
                _ => "Pendiente"
            },
            PagoEnProceso = cita.PagoEnProceso,
            MercadoPagoPublicKey = _configuration["MercadoPago:PublicKey"] ?? "",
            ClienteEmail = User.FindFirstValue(ClaimTypes.Email) ?? ""
        };

        return View("Resumen", vm);
    }

    [HttpPost("{id:int}/cupon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarCupon(int id, [FromForm] string codigo)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == id && c.ClienteId == clienteId);
        if (cita == null) return NotFound();

        if (cita.Estado != EstadoCita.Pendiente || cita.MetodoPago != null || cita.PagoEnProceso ||
            cita.PrecioTotal <= 0)
        {
            return BadRequest(new { success = false, message = "No se puede aplicar un cupón a esta reserva." });
        }

        if (!string.Equals(codigo?.Trim(), CodigoCuponValido, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "El cupón ingresado no es válido." });
        }

        var descuento = Math.Round(cita.PrecioBase * PorcentajeDescuentoCupon, 2, MidpointRounding.AwayFromZero);
        var total = cita.PrecioBase - descuento;
        var actualizadas = await _db.Citas
            .Where(c => c.Id == id
                && c.ClienteId == clienteId
                && c.Estado == EstadoCita.Pendiente
                && c.MetodoPago == null
                && !c.PagoEnProceso
                && c.PrecioTotal > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Descuento, descuento)
                .SetProperty(c => c.PrecioTotal, total)
                .SetProperty(c => c.CuponCodigo, CodigoCuponValido));

        if (actualizadas != 1)
        {
            return Conflict(new { success = false, message = "No se puede aplicar un cupón mientras el pago está en proceso." });
        }

        return Json(new
        {
            success = true,
            message = "Cupón aplicado correctamente",
            precioBase = cita.PrecioBase,
            descuento,
            total
        });
    }

    [HttpPost("{id:int}/pago-efectivo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarEnEfectivo(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == id && c.ClienteId == clienteId);
        if (cita == null) return NotFound();

        if (cita.Estado == EstadoCita.PendientePago && cita.MetodoPago == MetodoPagoCita.Efectivo)
        {
            TempData["Success"] = "Reserva realizada, realice el pago al asistir";
            return RedirectToAction(nameof(Resumen), new { id });
        }

        if (cita.Estado != EstadoCita.Pendiente || cita.MetodoPago != null || cita.PagoEnProceso)
        {
            TempData["Error"] = "Esta reserva ya tiene un pago registrado o en proceso.";
            return RedirectToAction(nameof(Resumen), new { id });
        }

        var actualizadas = await _db.Citas
            .Where(c => c.Id == id
                && c.ClienteId == clienteId
                && c.Estado == EstadoCita.Pendiente
                && c.MetodoPago == null
                && !c.PagoEnProceso)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.MetodoPago, MetodoPagoCita.Efectivo)
                .SetProperty(c => c.Estado, EstadoCita.PendientePago));

        if (actualizadas != 1)
        {
            TempData["Error"] = "Esta reserva ya tiene un pago registrado o en proceso.";
            return RedirectToAction(nameof(Resumen), new { id });
        }

        TempData["Success"] = "Reserva realizada, realice el pago al asistir";
        return RedirectToAction(nameof(Resumen), new { id });
    }

    [HttpPost("{id:int}/pago-tarjeta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarConTarjeta(int id, [FromBody] PagoTarjetaRequest? request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Forbid();

        var accessToken = _configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken) ||
            string.IsNullOrWhiteSpace(_configuration["MercadoPago:PublicKey"]))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { success = false, message = "El pago con tarjeta no está configurado." });
        }

        if (request == null || !ModelState.IsValid ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.PaymentMethodId) ||
            string.IsNullOrWhiteSpace(request.IdentificationNumber))
        {
            return BadRequest(new { success = false, message = "Los datos de la tarjeta están incompletos." });
        }

        var idempotencyKey = Guid.NewGuid().ToString();
        var reservado = await _db.Citas
            .Where(c => c.Id == id && c.ClienteId == clienteId
                && c.Estado == EstadoCita.Pendiente
                && c.MetodoPago == null
                && !c.PagoEnProceso
                && c.PrecioTotal > 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.PagoEnProceso, true));

        if (reservado != 1)
        {
            return Conflict(new { success = false, message = "La reserva no está disponible para pagar." });
        }

        var cita = await _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .FirstAsync(c => c.Id == id && c.ClienteId == clienteId);

        var paymentRequest = new
        {
            transaction_amount = cita.PrecioTotal,
            token = request.Token,
            description = $"Reserva {cita.Servicio!.Nombre}",
            installments = request.Installments,
            payment_method_id = request.PaymentMethodId,
            issuer_id = request.IssuerId,
            external_reference = cita.Id.ToString(),
            payer = new
            {
                email = cita.Cliente!.Email,
                identification = new
                {
                    type = request.IdentificationType,
                    number = request.IdentificationNumber
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/v1/payments")
        {
            Content = System.Net.Http.Json.JsonContent.Create(paymentRequest)
        };
        httpRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("X-Idempotency-Key", idempotencyKey);

        using var client = _httpClientFactory.CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(httpRequest);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con Mercado Pago para la cita {CitaId}", id);
            cita.PagoEnProceso = false;
            await _db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status502BadGateway,
                new { success = false, message = "No se pudo conectar con Mercado Pago. Intenta nuevamente." });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Tiempo de espera agotado al procesar el pago de la cita {CitaId}", id);
            cita.PagoEnProceso = false;
            await _db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                new { success = false, message = "Mercado Pago no respondió a tiempo. Intenta nuevamente." });
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            JsonDocument payment;
            try
            {
                payment = JsonDocument.Parse(responseBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Mercado Pago devolvió una respuesta inválida para la cita {CitaId}", id);
                cita.PagoEnProceso = false;
                await _db.SaveChangesAsync();
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { success = false, message = "No se pudo validar la respuesta de Mercado Pago." });
            }

            using (payment)
            {
                var root = payment.RootElement;
                var estadoPago = root.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString()
                    : null;
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Mercado Pago rechazó la solicitud de pago para la cita {CitaId} con HTTP {StatusCode}",
                        id, (int)response.StatusCode);
                }

                if (response.IsSuccessStatusCode && estadoPago == "approved" &&
                    root.TryGetProperty("id", out var operationElement))
                {
                    cita.PagoEnProceso = false;
                    cita.MetodoPago = MetodoPagoCita.Tarjeta;
                    cita.Estado = EstadoCita.Pagada;
                    cita.NumeroOperacion = operationElement.ToString();
                    cita.FechaPago = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Pago exitoso",
                        operacion = cita.NumeroOperacion,
                        redirectUrl = Url.Action(nameof(Resumen), new { id })
                    });
                }

                cita.PagoEnProceso = false;
                await _db.SaveChangesAsync();
                return BadRequest(new
                {
                    success = false,
                    message = "Mercado Pago no aprobó el pago. Verifica los datos o intenta con otra tarjeta."
                });
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  HELPER — Genera slots de tiempo según jornada del estilista
    // ────────────────────────────────────────────────────────────────
    private static List<TimeOnly> GenerarSlots(
        DateOnly fecha,
        TimeOnly inicioJornada, TimeOnly finJornada,
        TimeOnly? inicioDescanso, TimeOnly? finDescanso,
        int duracionMinutos)
    {
        var slots = new List<TimeOnly>();
        var cursor = inicioJornada;

        while (cursor.AddMinutes(duracionMinutos) <= finJornada)
        {
            var cursorFin = cursor.AddMinutes(duracionMinutos);

            // Saltar si cae dentro del descanso
            bool enDescanso = inicioDescanso.HasValue && finDescanso.HasValue
                && cursor < finDescanso.Value && cursorFin > inicioDescanso.Value;

            if (!enDescanso)
            {
                // No mostrar slots pasados (para el día de hoy)
                var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaSalon);
                var fechaHoyLocal = DateOnly.FromDateTime(ahoraLocal);
                if (fecha == fechaHoyLocal && cursor <= TimeOnly.FromDateTime(ahoraLocal))
                {
                    cursor = cursor.AddMinutes(30);
                    continue;
                }

                slots.Add(cursor);
            }

            cursor = cursor.AddMinutes(30); // intervalo de 30 min entre slots
        }

        return slots;
    }

    private static DateTime AFechaUtc(DateOnly fecha, TimeOnly hora) =>
        TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(hora), ZonaSalon);

    private static bool CoincideEspecialidad(string especialidad, string? categoria) =>
        !string.IsNullOrWhiteSpace(categoria)
        && string.Equals(especialidad.Trim(), categoria.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool EsFechaReservaValida(DateOnly fecha)
    {
        var hoyLocal = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaSalon));

        return fecha > hoyLocal
            && fecha <= FinVentanaReserva(hoyLocal)
            && fecha.DayOfWeek != DayOfWeek.Sunday;
    }

    private static DateOnly FinVentanaReserva(DateOnly hoyLocal) =>
        new DateOnly(hoyLocal.Year, hoyLocal.Month, 1)
            .AddMonths(MesesCalendarioReserva)
            .AddDays(-1);
}
