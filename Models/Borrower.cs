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

        // Navigation property for borrowing transactions
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
