using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using opn_chat.Domain.Interfaces;

namespace opn_chat.Infrastructure.Services
{
    public class RoomArchivalService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RoomArchivalService> _logger;

        public RoomArchivalService(IServiceScopeFactory scopeFactory, ILogger<RoomArchivalService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RoomArchival] Service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await ArchiveInactiveRoomsAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ArchiveInactiveRoomsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var roomRepo = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var cutoff = DateTime.UtcNow.AddDays(-30);
                var rooms = await roomRepo.GetInactiveForArchivalAsync(cutoff);
                var archived = 0;

                foreach (var room in rooms)
                {
                    room.IsArchived = true;
                    await roomRepo.UpdateAsync(room);
                    archived++;
                }

                if (archived > 0)
                {
                    await uow.CommitAsync();
                    _logger.LogInformation("[RoomArchival] Archived {Count} inactive room(s).", archived);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RoomArchival] Error during archival run.");
            }
        }
    }
}
