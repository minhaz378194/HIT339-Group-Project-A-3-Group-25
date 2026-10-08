using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Represents an automated system notification dispatched to a patron/borrower
    /// via simulated Email or SMS channels.
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Borrower Borrower { get; set; } = null!;

        /// <summary>
        /// Optional associated Loan ID to track event triggers and prevent duplicate alerts.
        /// </summary>
        [Display(Name = "Related Loan")]
        public int? LoanId { get; set; }

        [ForeignKey("LoanId")]
        public virtual Loan? Loan { get; set; }

        [Required]
        [Display(Name = "Channel")]
        public NotificationChannel Channel { get; set; } = NotificationChannel.Email;

        [Required]
        [Display(Name = "Notification Type")]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Message Content")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Status")]
        public NotificationStatus Status { get; set; } = NotificationStatus.Sent;

        [Required]
        [Display(Name = "Dispatched At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
