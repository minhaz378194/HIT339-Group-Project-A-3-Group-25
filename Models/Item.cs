using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Superclass for all library catalog items (Books, Music, Toys).
    /// Follows Object-Oriented inheritance pattern.
    /// </summary>
    public abstract class Item
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Library Code is required.")]
        [RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "Library code must be in the format 'XXX-0000' (e.g., BOK-1001, MUS-2001, TOY-3001).")]
        [Display(Name = "Library Code")]
        [StringLength(10)]
        public string LibraryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item Name / Title is required.")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        [Display(Name = "Title / Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Available;

        [Display(Name = "Cover / Graphic URL")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Replacement cost is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "Replacement cost must be between $0.01 and $10,000.00.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Replacement Cost ($)")]
        public decimal ReplacementCost { get; set; } = 25.00m;

        [Display(Name = "Added On")]
        [DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Branch assignment is required.")]
        [Display(Name = "Branch Location")]
        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;

        /// <summary>
        /// Discriminator property to identify the specific item type.
        /// </summary>
        [NotMapped]
        public abstract ItemType ItemType { get; }

        // Navigation property for loan history
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }

    /// <summary>
    /// Derived class representing a Book item in the library collection.
    /// </summary>
    public class Book : Item
    {
        [Required(ErrorMessage = "Author name is required.")]
        [StringLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Genre is required.")]
        [StringLength(50, ErrorMessage = "Genre cannot exceed 50 characters.")]
        public string Genre { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

        [Range(1, 10000, ErrorMessage = "Page count must be a positive number.")]
        [Display(Name = "Page Count")]
        public int? PageCount { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        [Range(1000, 2100, ErrorMessage = "Please enter a valid publication year.")]
        [Display(Name = "Publication Year")]
        public int? PublicationYear { get; set; }

        [NotMapped]
        public override ItemType ItemType => ItemType.Book;
    }

    /// <summary>
    /// Derived class representing a Music item (CD/Vinyl/Digital) in the library collection.
    /// </summary>
    public class Music : Item
    {
        [Required(ErrorMessage = "Artist or Composer is required.")]
        [StringLength(100, ErrorMessage = "Artist cannot exceed 100 characters.")]
        public string Artist { get; set; } = string.Empty;

        [Required(ErrorMessage = "Release Year is required.")]
        [Range(1900, 2100, ErrorMessage = "Please enter a valid release year.")]
        [Display(Name = "Release Year")]
        public int ReleaseYear { get; set; } = DateTime.UtcNow.Year;

        [StringLength(100)]
        [Display(Name = "Record Label")]
        public string? RecordLabel { get; set; }

        [StringLength(50)]
        [Display(Name = "Format (e.g. Vinyl, CD, Digital)")]
        public string? Format { get; set; } = "CD";

        [Range(1, 1000, ErrorMessage = "Duration must be between 1 and 1000 minutes.")]
        [Display(Name = "Duration (Minutes)")]
        public int? DurationMinutes { get; set; }

        [StringLength(50)]
        [Display(Name = "Music Genre")]
        public string? Genre { get; set; }

        [NotMapped]
        public override ItemType ItemType => ItemType.Music;
    }

    /// <summary>
    /// Derived class representing a Toy or Educational Game item in the library collection.
    /// </summary>
    public class Toy : Item
    {
        [Required(ErrorMessage = "Toy Type is required.")]
        [StringLength(50, ErrorMessage = "Toy Type cannot exceed 50 characters.")]
        [Display(Name = "Toy Category / Type")]
        public string ToyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Minimum recommended age is required.")]
        [Range(0, 100, ErrorMessage = "Minimum age must be between 0 and 100.")]
        [Display(Name = "Min Recommended Age (Years)")]
        public int RecommendedAgeMin { get; set; } = 3;

        [Range(0, 100, ErrorMessage = "Maximum age must be between 0 and 100.")]
        [Display(Name = "Max Recommended Age (Years)")]
        public int? RecommendedAgeMax { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [Range(1, 100000, ErrorMessage = "Piece count must be at least 1.")]
        [Display(Name = "Number of Pieces")]
        public int? PieceCount { get; set; }

        [StringLength(300)]
        [Display(Name = "Safety Warnings / Notes")]
        public string? SafetyNotes { get; set; }

        [NotMapped]
        public override ItemType ItemType => ItemType.Toy;
    }
}
