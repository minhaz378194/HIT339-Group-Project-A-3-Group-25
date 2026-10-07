using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraTrack.Models
{
    /// <summary>
    /// Tracks an inter-branch logistics transfer request for a physical catalog item.
    /// </summary>
    public class ItemTransfer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; } = null!;

        [Required(ErrorMessage = "Origin Branch is required.")]
        [Display(Name = "Origin Branch")]
        public int FromBranchId { get; set; }

        [ForeignKey("FromBranchId")]
        public virtual Branch FromBranch { get; set; } = null!;

        [Required(ErrorMessage = "Destination Branch is required.")]
        [Display(Name = "Destination Branch")]
        public int ToBranchId { get; set; }

        [ForeignKey("ToBranchId")]
        public virtual Branch ToBranch { get; set; } = null!;

        [Display(Name = "Requested By")]
        public int? RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public virtual UserAccount? RequestedByUser { get; set; }

        [Required]
        [Display(Name = "Requested Date")]
        [DataType(DataType.Date)]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Status")]
        public TransferStatus Status { get; set; } = TransferStatus.Requested;

        [Display(Name = "Completed Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletedAt { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Transfer Notes")]
        public string? Notes { get; set; }
    }
}
