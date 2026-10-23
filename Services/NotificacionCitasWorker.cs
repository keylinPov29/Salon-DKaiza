using DKaiza.Data;
using DKaiza.Web.Dominio;
using Microsoft.EntityFrameworkCore;

namespace DKaiza.Web.Services;

public sealed class NotificacionCitasWorker : BackgroundService
{
    private static readonly TimeSpan IntervaloRevision = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificacionCitasWorker> _logger;

    public NotificacionCitasWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificacionCitasWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloRevision);
        do
        {
            try
            {
                await CrearRecordatoriosVencidosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron generar los recordatorios de citas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CrearRecordatoriosVencidosAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ahora = DateTime.UtcNow;
        var limite = ahora.AddHours(24);

        var citas = await db.Citas
            .AsNoTracking()
            .Where(c => c.Inicio > ahora &&
                        c.Inicio <= limite &&
                        (c.Estado == EstadoCita.Pendiente ||
                         c.Estado == EstadoCita.PendientePago ||
                         c.Estado == EstadoCita.Pagada ||
                         c.Estado == EstadoCita.Confirmada))
            .Select(c => new { c.Id, c.ClienteId })
            .ToListAsync(cancellationToken);

        foreach (var cita in citas)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO "NotificacionesCita" ("CitaId", "ClienteId", "CreadaEn", "ResueltaEn")
                 VALUES ({cita.Id}, {cita.ClienteId}, {ahora}, NULL)
                 ON CONFLICT ("CitaId") DO NOTHING
                 """,
                cancellationToken);
        }
    }
}
