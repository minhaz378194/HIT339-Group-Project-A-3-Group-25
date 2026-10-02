using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;

namespace LibraTrack.Services
{
    /// <summary>
    /// Background worker service executing on a configured interval (default: every 60 seconds).
    /// Scans active loans to identify items due within 2 days or currently overdue,
    /// and generates automated simulated patron notifications without creating duplicates.
    /// </summary>
    public class LoanNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoanNotificationBackgroundService> _logger;

        public LoanNotificationBackgroundService(
            IServiceScopeFactory scopeFactory, 
            IConfiguration configuration, 
            ILogger<LoanNotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int?>("NotificationSettings:CheckIntervalSeconds") ?? 60;
            var dueSoonDays = _configuration.GetValue<int?>("NotificationSettings:DueSoonDays") ?? 2;

            _logger.LogInformation("LoanNotificationBackgroundService started. Interval: {Interval}s, DueSoon window: {Days} days.",
                intervalSeconds, dueSoonDays);

            // Brief startup pause to allow web host and DB seeding to complete
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndDispatchLoanNotificationsAsync(dueSoonDays, stoppingToken);
                    await CheckAndExpireReadyReservationsAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error occurred during loan and reservation background worker execution.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }

            _logger.LogInformation("LoanNotificationBackgroundService is stopping.");
        }

        private async Task CheckAndExpireReadyReservationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            await reservationService.CheckAndExpireReadyReservationsAsync(stoppingToken);
        }

        private async Task CheckAndDispatchLoanNotificationsAsync(int dueSoonDays, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var dueSoonThreshold = now.AddDays(dueSoonDays);

            // Fetch active (unreturned) loans
            var activeLoans = await context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .Where(l => l.ReturnDate == null && l.Borrower.IsActive)
                .ToListAsync(stoppingToken);

            int dueSoonCount = 0;
            int overdueCount = 0;

            foreach (var loan in activeLoans)
            {
                // 1. Check Due Soon (active loan due within 2 days, but not yet overdue)
                if (loan.DueDate > now && loan.DueDate <= dueSoonThreshold)
                {
                    // Strict deduplication: never create the same notification twice for the same loan and type
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n => n.LoanId == loan.Id && n.Type == NotificationType.DueSoon, stoppingToken);

                    if (!alreadyNotified)
                    {
                        await notificationService.SendDueSoonNotificationAsync(loan);
                        dueSoonCount++;
                    }
                }

                // 2. Check Overdue (due date has elapsed)
                if (loan.DueDate.Date < now.Date)
                {
                    // Strict deduplication: never create the same notification twice for the same loan and type
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n => n.LoanId == loan.Id && n.Type == NotificationType.Overdue, stoppingToken);

                    if (!alreadyNotified)
                    {
                        await notificationService.SendOverdueNotificationAsync(loan);
                        overdueCount++;
                    }
                }
            }

            if (dueSoonCount > 0 || overdueCount > 0)
            {
                _logger.LogInformation("LoanNotification cycle dispatched: {DueSoon} due soon, {Overdue} overdue notices.",
                    dueSoonCount, overdueCount);
            }
        }
    }
}
