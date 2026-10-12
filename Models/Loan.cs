using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Represents a borrowing transaction linking an Item to a Borrower.
    /// Tracks loan period, return event, condition assessment, and late-fee calculations.
    /// </summary>
    public class Loan
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

        [Required]
        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(14);

        [Display(Name = "Returned Date")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Late Fine ($)")]
        public decimal FineAmount { get; set; } = 0.00m;

        [Display(Name = "Fine Status")]
        public bool FinePaid { get; set; } = false;

        [StringLength(300)]
        [Display(Name = "Staff Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Condition at Return")]
        public ReturnCondition? ReturnedCondition { get; set; }

        /// <summary>
        /// True if the item has been returned back to the library.
        /// </summary>
        [NotMapped]
        public bool IsReturned => ReturnDate.HasValue;

        /// <summary>
        /// True if the loan is currently overdue or was returned past the due date.
        /// </summary>
        [NotMapped]
        public bool IsOverdue
        {
            get
            {
                var effectiveDate = ReturnDate ?? DateTime.UtcNow;
                return effectiveDate.Date > DueDate.Date;
            }
        }

        /// <summary>
        /// Number of days this loan is overdue.
        /// </summary>
        [NotMapped]
        public int OverdueDays
        {
            get
            {
                var effectiveDate = ReturnDate ?? DateTime.UtcNow;
                var days = (effectiveDate.Date - DueDate.Date).Days;
                return days > 0 ? days : 0;
            }
        }

        /// <summary>
        /// True if the loan is not returned, not overdue, but due within 2 days.
        /// </summary>
        [NotMapped]
        public bool IsDueSoon
        {
            get
            {
                if (IsReturned || IsOverdue) return false;
                var days = (DueDate.Date - DateTime.UtcNow.Date).Days;
                return days >= 0 && days <= 2;
            }
        }

        /// <summary>
        /// Number of days until this loan is due (0 if due today).
        /// </summary>
        [NotMapped]
        public int DaysUntilDue
        {
            get
            {
                var days = (DueDate.Date - DateTime.UtcNow.Date).Days;
                return days > 0 ? days : 0;
            }
        }

        /// <summary>
        /// Calculates the overdue fine amount using the standard library daily fine rate ($1.00 / day).
        /// </summary>
        public decimal CalculateDynamicFine(decimal dailyRate = 1.00m)
        {
            if (OverdueDays <= 0) return 0.00m;
            return Math.Round(OverdueDays * dailyRate, 2);
        }
    }
}
