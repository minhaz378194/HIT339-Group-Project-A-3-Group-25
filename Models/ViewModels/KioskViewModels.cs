using System.ComponentModel.DataAnnotations;
using LibraTrack.Models;

namespace LibraTrack.Models.ViewModels
{
    /// <summary>
    /// ViewModel for self-service kiosk patron authentication.
    /// </summary>
    public class KioskLoginViewModel
    {
        [Required(ErrorMessage = "Please scan or enter your Borrower Card Number.")]
        [Display(Name = "Borrower Card Number")]
        public string BorrowerCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your 4-digit PIN.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be 4 numeric digits.")]
        [DataType(DataType.Password)]
        public string Pin { get; set; } = string.Empty;

        public string TargetAction { get; set; } = "checkout"; // "checkout" or "account"

        public string? ErrorMessage { get; set; }

        public int RemainingAttempts { get; set; } = 3;

        public bool IsLocked { get; set; } = false;
    }

    /// <summary>
    /// ViewModel for self-service kiosk item scan and checkout confirmation.
    /// </summary>
    public class KioskCheckOutViewModel
    {
        public Borrower Borrower { get; set; } = null!;

        [Display(Name = "Item Library Code / Barcode")]
        public string? LibraryCode { get; set; }

        public Item? ScannedItem { get; set; }

        public bool CanConfirm { get; set; } = false;

        public string? HoldRestrictionMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public string? SuccessMessage { get; set; }

        public Loan? CompletedLoan { get; set; }
    }

    /// <summary>
    /// ViewModel for self-service kiosk 'My Account' patron overview.
    /// </summary>
    public class KioskAccountViewModel
    {
        public Borrower Borrower { get; set; } = null!;

        public List<Loan> ActiveLoans { get; set; } = new();

        public decimal TotalFinesOwed { get; set; }

        public List<Reservation> Reservations { get; set; } = new();
    }
}
