
using PMS.Application.Services.SalesQuotation;

namespace PMS.API.BackgroundTasks
{
    public class SalesQuotationStatusUpdater : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SalesQuotationStatusUpdater(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var quotationService = scope.ServiceProvider.GetRequiredService<ISalesQuotationService>();
                        await quotationService.UpdateExpiredQuotationAsync();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[BackgroundService Error] {ex.Message}");
                }

                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
