using Jetar.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// Fon vazifalari (arxitekturada Hangfire o'rni): muddati o'tgan escrow'larni
/// avtomatik chiqarish va to'lanmagan bitimlarni bekor qilish.
/// </summary>
public class EscrowBackgroundWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EscrowBackgroundWorker> _log;

    public EscrowBackgroundWorker(IServiceScopeFactory scopes, ILogger<EscrowBackgroundWorker> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ilova ko'tarilib bo'lishiga fursat beramiz.
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var escrow = scope.ServiceProvider.GetRequiredService<IEscrowService>();

                var released = await escrow.ReleaseExpiredAsync(stoppingToken);
                var cancelled = await escrow.CancelStaleUnpaidAsync(stoppingToken);

                if (released > 0 || cancelled > 0)
                    _log.LogInformation("Fon vazifasi: {Released} avto-release, {Cancelled} bekor qilindi",
                        released, cancelled);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Bitta xato tsiklni to'xtatmasligi kerak.
                _log.LogError(ex, "Escrow fon vazifasida xato");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
