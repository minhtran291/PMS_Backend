using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Application.Services.SalesOrder
{
    public class DepositOverdueBackgroundJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DepositOverdueBackgroundJob> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public DepositOverdueBackgroundJob(IServiceScopeFactory scopeFactory,
        ILogger<DepositOverdueBackgroundJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunOnce(stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnce(stoppingToken);
            }
        }

        private async Task RunOnce(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var salesOrderService =
                    scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

                var notiService =
                    scope.ServiceProvider.GetRequiredService<INotificationService>();

                var updated =
                    await salesOrderService.AutoMarkNotCompleteWhenDepositOverdueAsync();

                if (updated > 0)
                {
                    _logger.LogInformation(
                        "Job kiểm tra quá hạn cọc: đã chuyển {Count} đơn hàng sang trạng thái Không hoàn thành (NotComplete).",
                        updated);

                    try
                    {
                        //var senderId = "SYSTEM";
                        //var roles = new List<string> { "ACCOUNTANT" };

                        //await notiService.SendNotificationToRolesAsync(
                        //    senderId,
                        //    roles,
                        //    "Đơn hàng quá hạn cọc",
                        //    $"Hệ thống phát hiện {updated} đơn hàng quá hạn thanh toán cọc và đã chuyển sang trạng thái Không hoàn thành (NotComplete).",
                        //    NotificationType.Warning
                        //);

                        _logger.LogInformation(
                            "Job kiểm tra quá hạn cọc: đã gửi thông báo cho bộ phận kế toán.");
                    }
                    catch (Exception notiEx)
                    {
                        _logger.LogError(
                            notiEx,
                            "Job kiểm tra quá hạn cọc: gửi thông báo cho kế toán thất bại.");
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Job kiểm tra quá hạn cọc: không có đơn hàng nào cần cập nhật.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "Job kiểm tra quá hạn cọc đã dừng do hệ thống tắt.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Job kiểm tra quá hạn cọc gặp lỗi khi xử lý đơn hàng.");
            }
        }
    }
}
