using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DKaiza.Data;
using DKaiza.Web.Models;
using DKaiza.Web.Dominio;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminReportesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminReportesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? anio, DateTime? fechaInicio, DateTime? fechaFin, string? metodoPago)
    {
        var model = await ObtenerDatosReporte(anio, fechaInicio, fechaFin, metodoPago, esVistaPrevia: false);
        return View(model);
    }

    public async Task<IActionResult> VistaPreviaReporte(int? anio)
    {
        var model = await ObtenerDatosReporte(anio, null, null, null, esVistaPrevia: true);
        return PartialView("_VistaPreviaReporte", model);
    }

    private async Task<ReporteUnificadoViewModel> ObtenerDatosReporte(int? anio, DateTime? fechaInicio, DateTime? fechaFin, string? metodoPago, bool esVistaPrevia)
    {
        var anioActual = DateTime.Today.Year;
        var anioSeleccionado = anio ?? anioActual;
        
        var inicioAnioUtc = new DateTime(anioSeleccionado, 1, 1).ToUniversalTime();
        var finAnioUtc = new DateTime(anioSeleccionado, 12, 31, 23, 59, 59).ToUniversalTime();

        var model = new ReporteUnificadoViewModel
        {
            AnioSeleccionado = anioSeleccionado,
            AniosDisponibles = Enumerable.Range(anioActual - 5, 6).Reverse().ToList()
        };

        // --- 1. DATOS DEL AÑO COMPLETO ---
        var citasAnio = await _context.Citas
            .Include(c => c.Servicio)
            .Include(c => c.Estilista)
            .Include(c => c.Cliente)
            .Where(c => c.Estado == EstadoCita.Completada && c.Inicio >= inicioAnioUtc && c.Inicio <= finAnioUtc)
            .OrderByDescending(c => c.Inicio)
            .ToListAsync();

        var todosEstilistas = await _context.Estilistas.Where(e => e.Activo).ToListAsync();
        
        var todosServicios = _context.Servicios.Select(s => s.Nombre).Distinct().OrderBy(n => n).ToList();
        model.NombresServiciosColumnas = todosServicios;

        if (citasAnio.Any())
        {
            // Estadísticas del año
            var serviciosPorMes = new int[12];
            foreach (var cita in citasAnio) { int mes = cita.Inicio.ToLocalTime().Month - 1; serviciosPorMes[mes]++; }
            model.ServiciosPorMes = serviciosPorMes.ToList();

            model.EstilistaTop = citasAnio.GroupBy(c => c.Estilista?.NombreCompleto ?? "N/A")
                .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "N/A";
            
            model.ServicioTop = citasAnio.GroupBy(c => c.Servicio?.Nombre ?? "N/A")
                .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "N/A";

            // 👇 CORRECCIÓN AQUÍ: Calculamos los Top Servicios con TODAS las citas del año, no solo del mes actual 👇
            model.ServiciosDelMes = citasAnio.GroupBy(c => c.Servicio?.Nombre ?? "N/A")
                .Select(g => new RankingDto { 
                    Nombre = g.Key, 
                    Cantidad = g.Count(), 
                    Porcentaje = Math.Round((double)g.Count() / citasAnio.Count * 100, 1) 
                })
                .OrderByDescending(r => r.Cantidad)
                .Take(4)
                .ToList();

            // Tabla por estilista
            model.TablaEstilistas = todosEstilistas.Select(est => {
                var citasEst = citasAnio.Where(c => c.EstilistaId == est.Id).ToList();
                var dictServicios = new Dictionary<string, int>();
                foreach (var nombreServ in model.NombresServiciosColumnas)
                {
                    dictServicios[nombreServ] = citasEst.Count(c => c.Servicio?.Nombre == nombreServ);
                }
                return new EstilistaResumenDto {
                    Nombre = est.NombreCompleto,
                    Iniciales = $"{(est.Nombres?.Length > 0 ? est.Nombres[0].ToString() : "")}{(est.Apellidos?.Length > 0 ? est.Apellidos[0].ToString() : "")}",
                    ServiciosRealizados = dictServicios,
                    Total = citasEst.Count,
                    Ingresos = citasEst.Sum(c => c.PrecioTotal)
                };
            }).OrderByDescending(e => e.Total).ToList();

            // Desglose Mensual Real
            model.DesgloseMensualReal = new List<DesgloseMesDto>();
            for (int i = 1; i <= 12; i++)
            {
                var citasDelMes = citasAnio.Where(c => c.Inicio.ToLocalTime().Month == i).ToList();
                var mesDto = new DesgloseMesDto
                {
                    MesNombre = new DateTime(2000, i, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES")),
                    TotalCitas = citasDelMes.Count,
                    TotalIngresos = citasDelMes.Sum(c => c.PrecioTotal),
                    CantidadPorServicio = new Dictionary<string, int>()
                };
                foreach (var nombreServ in model.NombresServiciosColumnas)
                {
                    mesDto.CantidadPorServicio[nombreServ] = citasDelMes.Count(c => c.Servicio?.Nombre == nombreServ);
                }
                model.DesgloseMensualReal.Add(mesDto);
            }
        }

        // --- 2. LÓGICA DE INGRESOS (Depende de si es Vista Previa o Pantalla Principal) ---
        if (esVistaPrevia)
        {
            var todasTransacciones = citasAnio.Select(c => new TransaccionDto
            {
                CitaId = c.Id,
                FechaTransaccion = c.HoraFinReal?.ToLocalTime() ?? c.Inicio.ToLocalTime(),
                Cliente = $"{c.Cliente?.Nombre} {c.Cliente?.Apellido}",
                Servicio = c.Servicio?.Nombre ?? "N/A",
                Monto = c.PrecioTotal,
                MetodoPago = c.Id % 2 == 0 ? "Tarjeta" : "Efectivo" 
            }).ToList();

            model.TotalEfectivo = todasTransacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Monto);
            model.TotalTarjeta = todasTransacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Monto);
            model.GranTotal = model.TotalEfectivo + model.TotalTarjeta;
            model.CantidadServicios = todasTransacciones.Count;
            model.Transacciones = todasTransacciones;
            model.FechaInicio = inicioAnioUtc.ToLocalTime();
            model.FechaFin = finAnioUtc.ToLocalTime();
        }
        else
        {
            var inicio = fechaInicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fin = fechaFin ?? DateTime.Today;
            var inicioIngresosUtc = inicio.Date.ToUniversalTime();
            var finIngresosUtc = fin.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();

            var citasFiltradas = await _context.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .Where(c => c.Estado == EstadoCita.Completada && c.Inicio >= inicioIngresosUtc && c.Inicio <= finIngresosUtc)
                .OrderByDescending(c => c.Inicio)
                .ToListAsync();

            var transaccionesFiltradas = citasFiltradas.Select(c => new TransaccionDto
            {
                CitaId = c.Id,
                FechaTransaccion = c.HoraFinReal?.ToLocalTime() ?? c.Inicio.ToLocalTime(),
                Cliente = $"{c.Cliente?.Nombre} {c.Cliente?.Apellido}",
                Servicio = c.Servicio?.Nombre ?? "N/A",
                Monto = c.PrecioTotal,
                MetodoPago = c.Id % 2 == 0 ? "Tarjeta" : "Efectivo" 
            }).ToList();

            model.TotalEfectivo = transaccionesFiltradas.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Monto);
            model.TotalTarjeta = transaccionesFiltradas.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Monto);
            model.GranTotal = model.TotalEfectivo + model.TotalTarjeta;

            if (!string.IsNullOrEmpty(metodoPago))
            {
                transaccionesFiltradas = transaccionesFiltradas.Where(t => t.MetodoPago == metodoPago).ToList();
            }

            model.Transacciones = transaccionesFiltradas;
            model.CantidadServicios = model.Transacciones.Count;
            model.FechaInicio = inicio;
            model.FechaFin = fin;
        }

        return model;
    }
}