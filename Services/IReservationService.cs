using LibraTrack.Models;

namespace LibraTrack.Services
{
    /// <summary>
    /// Service contract managing item hold queues, ready-shelf promotions,
    /// expiration timeouts, and position re-sequencing.
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Finds the first waiting reservation for an item, transitions it to Ready,
        /// sets ExpiresAt to 3 days in the future, sends a ReservationReady notification,
        /// and re-sequences the remaining waiting queue.
        /// </summary>
        Task<Reservation?> PromoteNextWaitingReservationAsync(int itemId);

        /// <summary>
        /// Re-indexes queue positions (1, 2, 3...) for all waiting reservations of a given item.
        /// </summary>
        Task ResequenceQueueAsync(int itemId);

        /// <summary>
        /// Scans for Ready reservations whose hold window has elapsed (ExpiresAt < Now),
        /// marks them Expired, and promotes the next waiting patron in line.
        /// </summary>
        Task<int> CheckAndExpireReadyReservationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the active ready reservation for an item, if any.
        /// </summary>
        Task<Reservation?> GetActiveReadyReservationAsync(int itemId);

        /// <summary>
        /// Returns the count of patrons currently waiting in queue for an item.
        /// </summary>
        Task<int> GetWaitingQueueCountAsync(int itemId);
    }
}
