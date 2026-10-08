using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;

namespace LibraTrack.Services
{
    /// <summary>
    /// Implements reservation queue management, ready promotions, and hold timeouts.
    /// </summary>
    public class ReservationService : IReservationService
    {
        private readonly LibraryDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            LibraryDbContext context, 
            INotificationService notificationService,
            ILogger<ReservationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Reservation?> PromoteNextWaitingReservationAsync(int itemId)
        {
            // If there is already an unexpired Ready reservation for this item, keep it as is
            var existingReady = await _context.Reservations
                .AnyAsync(r => r.ItemId == itemId && 
                               r.Status == ReservationStatus.Ready && 
                               (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));

            if (existingReady)
            {
                return null;
            }

            // Find the highest priority waiting patron
            var nextReservation = await _context.Reservations
                .Include(r => r.Borrower)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Branch)
                .Where(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting)
                .OrderBy(r => r.QueuePosition)
                .ThenBy(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextReservation == null)
            {
                return null;
            }

            // Requirement 3: set to Ready, set ExpiresAt to 3 days later, and send ReservationReady notification
            nextReservation.Status = ReservationStatus.Ready;
            nextReservation.ReadyAt = DateTime.UtcNow;
            nextReservation.ExpiresAt = DateTime.UtcNow.AddDays(3);
            nextReservation.QueuePosition = 0;

            await _context.SaveChangesAsync();

            var branchName = nextReservation.Item?.Branch?.Name ?? "Library Desk";
            var itemName = nextReservation.Item?.Name ?? "Reserved Item";

            _logger.LogInformation("Promoted reservation #{Id} to Ready for patron {Patron} on item '{Item}' until {Expires:u}.",
                nextReservation.Id, nextReservation.Borrower?.FullName, itemName, nextReservation.ExpiresAt);

            // Dispatch notification
            try
            {
                await _notificationService.SendReservationReadyNotificationAsync(
                    nextReservation.BorrowerId,
                    itemName,
                    branchName,
                    NotificationChannel.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send ReservationReady notification for reservation #{Id}.", nextReservation.Id);
            }

            // Re-order remaining waiting patrons
            await ResequenceQueueAsync(itemId);

            return nextReservation;
        }

        public async Task ResequenceQueueAsync(int itemId)
        {
            var waiting = await _context.Reservations
                .Where(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting)
                .OrderBy(r => r.QueuePosition)
                .ThenBy(r => r.CreatedAt)
                .ToListAsync();

            for (int i = 0; i < waiting.Count; i++)
            {
                waiting[i].QueuePosition = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> CheckAndExpireReadyReservationsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Find Ready reservations where expiration window has passed
            var expiredReservations = await _context.Reservations
                .Include(r => r.Item)
                .Include(r => r.Borrower)
                .Where(r => r.Status == ReservationStatus.Ready && 
                            r.ExpiresAt.HasValue && 
                            r.ExpiresAt.Value < now)
                .ToListAsync(cancellationToken);

            int expiredCount = 0;

            foreach (var expired in expiredReservations)
            {
                expired.Status = ReservationStatus.Expired;
                await _context.SaveChangesAsync(cancellationToken);
                expiredCount++;

                _logger.LogInformation("Reservation #{Id} for patron {Borrower} on '{Item}' has EXPIRED.",
                    expired.Id, expired.Borrower?.FullName, expired.Item?.Name);

                // Requirement 7: Expires Ready reservations and notifies the next person
                await PromoteNextWaitingReservationAsync(expired.ItemId);
            }

            return expiredCount;
        }

        public async Task<Reservation?> GetActiveReadyReservationAsync(int itemId)
        {
            return await _context.Reservations
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.ItemId == itemId && 
                                          r.Status == ReservationStatus.Ready && 
                                          (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));
        }

        public async Task<int> GetWaitingQueueCountAsync(int itemId)
        {
            return await _context.Reservations
                .CountAsync(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting);
        }
    }
}
