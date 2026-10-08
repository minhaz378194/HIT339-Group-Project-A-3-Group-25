using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Represents a patron's hold/reservation request for a currently unavailable
    /// (Borrowed or Damaged) catalog item. Tracks queue position and ready/expiry windows.
    /// </summary>
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; } = null!;

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Borrower Borrower { get; set; } = null!;

        /// <summary>
        /// 1-based sequential position within the waiting queue for this specific item.
        /// Becomes 0 or inactive once the reservation transitions to Ready, Fulfilled, Cancelled, or Expired.
        /// </summary>
        [Display(Name = "Queue Position")]
        public int QueuePosition { get; set; } = 1;

        [Required]
        [Display(Name = "Reservation Status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

        [Required]
        [Display(Name = "Placed At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The exact UTC timestamp when the item was returned and placed on the hold shelf.
        /// </summary>
        [Display(Name = "Ready For Pickup At")]
        [DataType(DataType.DateTime)]
        public DateTime? ReadyAt { get; set; }

        /// <summary>
        /// The expiration timestamp (typically 3 days after becoming Ready).
        /// If not collected by this date, the hold expires and the next patron in queue is notified.
        /// </summary>
        [Display(Name = "Hold Expires At")]
        [DataType(DataType.DateTime)]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// True if this hold is currently in Ready status and has not passed its expiration date.
        /// </summary>
        [NotMapped]
        public bool IsActiveReady => Status == ReservationStatus.Ready && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
    }
}
