using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Represents a registered library patron / public borrower.
    /// </summary>
    public class Borrower
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Borrower Card Number is required.")]
        [RegularExpression(@"^BRW-\d{4}$", ErrorMessage = "Card number must follow the format 'BRW-0000' (e.g., BRW-1001).")]
        [Display(Name = "Card Number")]
        [StringLength(10)]
        public string BorrowerCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        [Display(Name = "Borrower Name")]
        public string FullName => $"{FirstName} {LastName}";

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Residential address is required.")]
        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Member Since")]
        [DataType(DataType.Date)]
        public DateTime MembershipDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Membership Status")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// SHA-256 hash of the borrower's 4-digit PIN for self-service kiosk authentication.
        /// </summary>
        [Display(Name = "PIN Hash")]
        public string? PinHash { get; set; }

        /// <summary>
        /// Consecutive failed PIN entry count; locked out after 3 failed attempts.
        /// </summary>
        public int FailedPinAttempts { get; set; } = 0;

        /// <summary>
        /// True if the borrower's kiosk access has been locked due to excessive failed attempts.
        /// </summary>
        [Display(Name = "Kiosk Locked Out")]
        public bool IsLockedOut { get; set; } = false;

        // Navigation property for borrowing transactions
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

        /// <summary>
        /// Hashes a 4-digit PIN using SHA256 with a project salt.
        /// </summary>
        public static string HashPin(string pin)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes("LibraTrackPinSalt_" + pin.Trim());
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verifies a 4-digit PIN against the stored PinHash using constant-time comparison.
        /// </summary>
        public bool VerifyPin(string pin)
        {
            if (string.IsNullOrEmpty(PinHash) || string.IsNullOrEmpty(pin))
                return false;

            try
            {
                var expectedBytes = Convert.FromBase64String(PinHash);
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var actualBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("LibraTrackPinSalt_" + pin.Trim()));
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
