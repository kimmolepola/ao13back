using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ao13back.Src;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RefreshTokenCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run forever until app shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-7); // keep 7 days for audit
                    var oldTokens = db.RefreshTokens
                        .Where(t => t.Expires < cutoff)
                        .ToList();

                    if (oldTokens.Any())
                    {
                        db.RefreshTokens.RemoveRange(oldTokens);
                        await db.SaveChangesAsync(stoppingToken);
                        Console.WriteLine($"[Cleanup] Removed {oldTokens.Count} old refresh tokens at {DateTime.UtcNow}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Error: {ex.Message}");
            }

            // Wait 24 hours before running again
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
