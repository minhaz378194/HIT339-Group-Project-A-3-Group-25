using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace LibraTrack.Models
{
    /// <summary>
    /// Authenticated library staff account.
    /// </summary>
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Staff Role")]
        public UserRole Role { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Staff Full Name")]
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Generates a SHA256 hashed password with a salt for secure storage.
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes("LibraTrackSalt_" + password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verifies a plain text password against the stored hash using constant-time comparison.
        /// </summary>
        public bool VerifyPassword(string password)
        {
            try
            {
                var expectedBytes = Convert.FromBase64String(PasswordHash);
                using var sha256 = SHA256.Create();
                var actualBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("LibraTrackSalt_" + password));
                return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
