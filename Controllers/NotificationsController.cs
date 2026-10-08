using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;
using LibraTrack.Services;

namespace LibraTrack.Controllers
{
    /// <summary>
    /// Staff dashboard controller for inspecting automated simulated patron notifications.
    /// Accessible exclusively to Admin and Manager roles.
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    public class NotificationsController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationsController(LibraryDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: /Notifications
        public async Task<IActionResult> Index(NotificationType? type, NotificationChannel? channel, string? search)
        {
            var baseQuery = _context.Notifications
                .Include(n => n.Borrower)
                .Include(n => n.Loan)
                    .ThenInclude(l => l!.Item)
                .AsQueryable();

            // Calculate metric counts before applying active search filters
            var totalSent = await _context.Notifications.CountAsync();
            var emailCount = await _context.Notifications.CountAsync(n => n.Channel == NotificationChannel.Email);
            var smsCount = await _context.Notifications.CountAsync(n => n.Channel == NotificationChannel.SMS);

            var borrowedCount = await _context.Notifications.CountAsync(n => n.Type == NotificationType.Borrowed);
            var dueSoonCount = await _context.Notifications.CountAsync(n => n.Type == NotificationType.DueSoon);
            var overdueCount = await _context.Notifications.CountAsync(n => n.Type == NotificationType.Overdue);
            var fineCount = await _context.Notifications.CountAsync(n => n.Type == NotificationType.FineIssued);
            var reservationCount = await _context.Notifications.CountAsync(n => n.Type == NotificationType.ReservationReady);

            // Apply filter by Type
            if (type.HasValue)
            {
                baseQuery = baseQuery.Where(n => n.Type == type.Value);
            }

            // Apply filter by Channel
            if (channel.HasValue)
            {
                baseQuery = baseQuery.Where(n => n.Channel == channel.Value);
            }

            // Apply text search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                baseQuery = baseQuery.Where(n =>
                    n.Subject.ToLower().Contains(term) ||
                    n.Message.ToLower().Contains(term) ||
                    n.Borrower.FirstName.ToLower().Contains(term) ||
                    n.Borrower.LastName.ToLower().Contains(term) ||
                    n.Borrower.BorrowerCardNumber.ToLower().Contains(term) ||
                    n.Borrower.Email.ToLower().Contains(term) ||
                    (n.Loan != null && n.Loan.Item.Name.ToLower().Contains(term)) ||
                    (n.Loan != null && n.Loan.Item.LibraryCode.ToLower().Contains(term)));
            }

            var notifications = await baseQuery
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var viewModel = new NotificationDashboardViewModel
            {
                Notifications = notifications,
                SelectedType = type,
                SelectedChannel = channel,
                SearchTerm = search,
                TotalSentCount = totalSent,
                EmailCount = emailCount,
                SmsCount = smsCount,
                BorrowedCount = borrowedCount,
                DueSoonCount = dueSoonCount,
                OverdueCount = overdueCount,
                FineIssuedCount = fineCount,
                ReservationReadyCount = reservationCount
            };

            return View(viewModel);
        }

        // POST: /Notifications/RunManualCheck
        // Convenience endpoint for staff/examiner to instantly run the due-soon and overdue scanner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunManualCheck()
        {
            var now = DateTime.UtcNow;
            var dueSoonThreshold = now.AddDays(2);

            var activeLoans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .Where(l => l.ReturnDate == null && l.Borrower.IsActive)
                .ToListAsync();

            int triggeredCount = 0;

            foreach (var loan in activeLoans)
            {
                if (loan.DueDate > now && loan.DueDate <= dueSoonThreshold)
                {
                    bool alreadySent = await _context.Notifications
                        .AnyAsync(n => n.LoanId == loan.Id && n.Type == NotificationType.DueSoon);

                    if (!alreadySent)
                    {
                        await _notificationService.SendDueSoonNotificationAsync(loan);
                        triggeredCount++;
                    }
                }

                if (loan.DueDate.Date < now.Date)
                {
                    bool alreadySent = await _context.Notifications
                        .AnyAsync(n => n.LoanId == loan.Id && n.Type == NotificationType.Overdue);

                    if (!alreadySent)
                    {
                        await _notificationService.SendOverdueNotificationAsync(loan);
                        triggeredCount++;
                    }
                }
            }

            TempData["Success"] = $"Manual scanner executed. {triggeredCount} new notification(s) dispatched.";
            return RedirectToAction(nameof(Index));
        }
    }
}
