using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;

namespace LibraTrack.Services
{
    /// <summary>
    /// Simulated patron notification service.
    /// Persists notification logs into SQLite and outputs formatted console alerts.
    /// Does not dispatch actual SMTP emails or SMS gateways.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(LibraryDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification> SendNotificationAsync(
            int borrowerId, 
            NotificationChannel channel, 
            NotificationType type, 
            string subject, 
            string message, 
            int? loanId = null)
        {
            var notification = new Notification
            {
                BorrowerId = borrowerId,
                LoanId = loanId,
                Channel = channel,
                Type = type,
                Subject = subject,
                Message = message,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Fetch borrower details for clear console output
            var borrower = await _context.Borrowers.FindAsync(borrowerId);
            var recipient = channel == NotificationChannel.Email 
                ? (borrower?.Email ?? "no-email@patron.local") 
                : (borrower?.Phone ?? "0400000000");
            var patronName = borrower?.FullName ?? $"Patron #{borrowerId}";

            // Console output matching Requirement 2
            Console.ForegroundColor = channel == NotificationChannel.Email ? ConsoleColor.Cyan : ConsoleColor.Magenta;
            Console.WriteLine($"[SIMULATED {channel.ToString().ToUpper()} NOTIFICATION] ➔ Recipient: {patronName} <{recipient}> | Type: {type} | Subject: '{subject}' | Message: \"{message}\"");
            Console.ResetColor();

            _logger.LogInformation("[SIMULATED {Channel} NOTIFICATION] Recipient: {Patron} <{Recipient}> | Type: {Type} | Subject: {Subject}",
                channel, patronName, recipient, type, subject);

            return notification;
        }

        public async Task SendItemIssuedNotificationAsync(Loan loan)
        {
            if (loan.Borrower == null)
            {
                await _context.Entry(loan).Reference(l => l.Borrower).LoadAsync();
            }
            if (loan.Item == null)
            {
                await _context.Entry(loan).Reference(l => l.Item).LoadAsync();
            }

            var itemName = loan.Item?.Name ?? "Item";
            var itemCode = loan.Item?.LibraryCode ?? "N/A";
            var borrowerName = loan.Borrower?.FullName ?? "Patron";

            var subject = $"Item Issued: {itemName} [{itemCode}]";
            var message = $"Hello {borrowerName}, you have borrowed '{itemName}' ({itemCode}) from the library on {loan.BorrowDate:dd MMM yyyy}. Your due date is {loan.DueDate:dddd, dd MMMM yyyy}. Please return it on time to avoid late fees.";

            await SendNotificationAsync(
                borrowerId: loan.BorrowerId,
                channel: NotificationChannel.Email,
                type: NotificationType.Borrowed,
                subject: subject,
                message: message,
                loanId: loan.Id);
        }

        public async Task SendFineIssuedNotificationAsync(Loan loan, decimal fineAmount)
        {
            if (loan.Borrower == null)
            {
                await _context.Entry(loan).Reference(l => l.Borrower).LoadAsync();
            }
            if (loan.Item == null)
            {
                await _context.Entry(loan).Reference(l => l.Item).LoadAsync();
            }

            var itemName = loan.Item?.Name ?? "Item";
            var borrowerName = loan.Borrower?.FullName ?? "Patron";
            var reason = loan.ReturnedCondition.HasValue && loan.ReturnedCondition != ReturnCondition.Good
                ? $"item returned in {loan.ReturnedCondition.Value.ToString().ToLower()} condition"
                : $"late return of {loan.OverdueDays} days past due date ({loan.DueDate:dd MMM yyyy})";

            var subject = $"Fine Assessed: ${fineAmount:F2} for '{itemName}'";
            var message = $"Hello {borrowerName}, a fine of ${fineAmount:F2} has been recorded for loan #{loan.Id} ('{itemName}') due to {reason}. Status: {(loan.FinePaid ? "PAID" : "UNPAID")}. Please settle any unpaid balance at the library circulation desk.";

            await SendNotificationAsync(
                borrowerId: loan.BorrowerId,
                channel: NotificationChannel.Email,
                type: NotificationType.FineIssued,
                subject: subject,
                message: message,
                loanId: loan.Id);
        }

        public async Task SendDueSoonNotificationAsync(Loan loan)
        {
            if (loan.Borrower == null)
            {
                await _context.Entry(loan).Reference(l => l.Borrower).LoadAsync();
            }
            if (loan.Item == null)
            {
                await _context.Entry(loan).Reference(l => l.Item).LoadAsync();
            }

            var itemName = loan.Item?.Name ?? "Item";
            var borrowerName = loan.Borrower?.FullName ?? "Patron";

            var subject = $"Reminder: '{itemName}' is due on {loan.DueDate:dd MMM yyyy}";
            var message = $"Hello {borrowerName}, this is a reminder that '{itemName}' is due in 2 days on {loan.DueDate:dddd, dd MMM yyyy}. Please return it or renew your loan to avoid late penalties.";

            // Use SMS for quick mobile due reminders
            await SendNotificationAsync(
                borrowerId: loan.BorrowerId,
                channel: NotificationChannel.SMS,
                type: NotificationType.DueSoon,
                subject: subject,
                message: message,
                loanId: loan.Id);
        }

        public async Task SendOverdueNotificationAsync(Loan loan)
        {
            if (loan.Borrower == null)
            {
                await _context.Entry(loan).Reference(l => l.Borrower).LoadAsync();
            }
            if (loan.Item == null)
            {
                await _context.Entry(loan).Reference(l => l.Item).LoadAsync();
            }

            var itemName = loan.Item?.Name ?? "Item";
            var borrowerName = loan.Borrower?.FullName ?? "Patron";
            var overdueDays = loan.OverdueDays;

            var subject = $"OVERDUE ALERT: '{itemName}' was due on {loan.DueDate:dd MMM yyyy}";
            var message = $"URGENT: Hello {borrowerName}, your loan for '{itemName}' is now {overdueDays} day(s) overdue. Daily late fines of $1.00/day are being incurred. Please return this item to any branch immediately.";

            await SendNotificationAsync(
                borrowerId: loan.BorrowerId,
                channel: NotificationChannel.Email,
                type: NotificationType.Overdue,
                subject: subject,
                message: message,
                loanId: loan.Id);
        }

        public async Task SendReservationReadyNotificationAsync(int borrowerId, string itemName, string branchName, NotificationChannel channel = NotificationChannel.Email)
        {
            var borrower = await _context.Borrowers.FindAsync(borrowerId);
            var borrowerName = borrower?.FullName ?? "Patron";

            var subject = $"Hold Available: '{itemName}' is ready for pickup";
            var message = $"Good news {borrowerName}! Your hold on '{itemName}' has arrived at {branchName} and is waiting on the hold shelf. Please collect it within 3 days (by your hold expiration date).";

            await SendNotificationAsync(
                borrowerId: borrowerId,
                channel: channel,
                type: NotificationType.ReservationReady,
                subject: subject,
                message: message,
                loanId: null);
        }
    }
}
