using LibraTrack.Models;

namespace LibraTrack.Services
{
    /// <summary>
    /// Service contract for dispatching simulated patron notifications (Email & SMS).
    /// Logs dispatched messages to the database and prints clear formatted alerts to the console.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a generic notification, records it to the database, and outputs to the console.
        /// </summary>
        Task<Notification> SendNotificationAsync(
            int borrowerId, 
            NotificationChannel channel, 
            NotificationType type, 
            string subject, 
            string message, 
            int? loanId = null);

        /// <summary>
        /// Sends a notification when a new loan is issued to a patron.
        /// </summary>
        Task SendItemIssuedNotificationAsync(Loan loan);

        /// <summary>
        /// Sends a notification when a returned item incurs a fine.
        /// </summary>
        Task SendFineIssuedNotificationAsync(Loan loan, decimal fineAmount);

        /// <summary>
        /// Sends a reminder notification when an active loan is due within 2 days.
        /// </summary>
        Task SendDueSoonNotificationAsync(Loan loan);

        /// <summary>
        /// Sends an urgent overdue alert when an active loan passes its due date.
        /// </summary>
        Task SendOverdueNotificationAsync(Loan loan);

        /// <summary>
        /// Sends an alert when a reserved item becomes ready for pickup (for future reservations).
        /// </summary>
        Task SendReservationReadyNotificationAsync(int borrowerId, string itemName, string branchName, NotificationChannel channel = NotificationChannel.Email);
    }
}
