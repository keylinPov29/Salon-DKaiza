using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DKaiza.Data;
using DKaiza.Web.Dominio;

namespace DKaiza.Web.Controllers;

[Authorize(Roles = "Administrador")] // Solo Admin puede entrar
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoyUtc = DateTime.UtcNow.Date;
        var finHoyUtc = hoyUtc.AddDays(1);
        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToUniversalTime();

        // Datos para las tarjetas
        ViewBag.CitasHoy = await _context.Citas
            .Where(c => c.Inicio >= hoyUtc && c.Inicio < finHoyUtc && c.Estado != EstadoCita.Cancelada)
            .CountAsync();

        ViewBag.IngresosMes = await _context.Citas
            .Where(c => c.Estado == EstadoCita.Completada && c.Inicio >= inicioMes)
            .SumAsync(c => c.PrecioTotal);

        ViewBag.EstilistasActivos = await _context.Estilistas.CountAsync(e => e.Activo);

        return View();
    }
}