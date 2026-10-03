using System.ComponentModel.DataAnnotations;

namespace LibraTrack.Models
{
    /// <summary>
    /// Audit log recording catalog data import executions (CSV imports and metadata provider imports).
    /// </summary>
    public class ImportLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Imported By")]
        public string Username { get; set; } = "system";

        [Display(Name = "Imported At")]
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        [Display(Name = "Source")]
        public string Source { get; set; } = "CSV Upload"; // "CSV Upload" | "Mock Metadata Search"

        [Display(Name = "Total Records")]
        public int TotalRows { get; set; }

        [Display(Name = "Imported")]
        public int ImportedCount { get; set; }

        [Display(Name = "Skipped")]
        public int SkippedCount { get; set; }

        [Display(Name = "Errors")]
        public int ErrorCount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// JSON-serialized error report data for downloading error reports after import.
        /// </summary>
        public string? ErrorReportJson { get; set; }
    }
}
