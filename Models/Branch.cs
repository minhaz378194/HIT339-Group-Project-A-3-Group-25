using System.ComponentModel.DataAnnotations;

namespace LibraTrack.Models
{
    /// <summary>
    /// Represents a physical branch location of the library network.
    /// </summary>
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Branch Name is required.")]
        [StringLength(100, ErrorMessage = "Branch name cannot exceed 100 characters.")]
        [Display(Name = "Branch Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Branch Code is required.")]
        [StringLength(20, ErrorMessage = "Branch code cannot exceed 20 characters.")]
        [Display(Name = "Branch Code")]
        public string BranchCode { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        [Display(Name = "Physical Address")]
        public string? Address { get; set; }

        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Contact Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        // Navigation property for catalog items currently located at this branch
        public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
