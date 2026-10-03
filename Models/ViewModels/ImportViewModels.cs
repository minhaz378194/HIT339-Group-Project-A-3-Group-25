using System.ComponentModel.DataAnnotations;
using LibraTrack.Services;

namespace LibraTrack.Models.ViewModels
{
    /// <summary>
    /// Represents a parsed row from an uploaded CSV import file.
    /// </summary>
    public class CsvItemRow
    {
        public int RowNumber { get; set; }

        // Common catalog fields
        public string ItemType { get; set; } = string.Empty;
        public string LibraryCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public decimal? ReplacementCost { get; set; }
        public string? ImageUrl { get; set; }

        // Book fields
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? ISBN { get; set; }
        public int? PageCount { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }

        // Music fields
        public string? Artist { get; set; }
        public int? ReleaseYear { get; set; }
        public string? RecordLabel { get; set; }
        public string? Format { get; set; }
        public int? DurationMinutes { get; set; }
        public string? MusicGenre { get; set; }

        // Toy fields
        public string? ToyType { get; set; }
        public int? RecommendedAgeMin { get; set; }
        public int? RecommendedAgeMax { get; set; }
        public string? Brand { get; set; }
        public int? PieceCount { get; set; }
        public string? SafetyNotes { get; set; }

        // Validation status
        public bool IsValid { get; set; }
        public string? ErrorReason { get; set; }
        public int? ResolvedBranchId { get; set; }
        public string? ResolvedBranchName { get; set; }
    }

    /// <summary>
    /// Preview view model displaying parsed rows and validity indicators before confirmation.
    /// </summary>
    public class ImportPreviewViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<CsvItemRow> Rows { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for the main /Import landing page.
    /// </summary>
    public class ImportIndexViewModel
    {
        public List<ImportLog> RecentLogs { get; set; } = new();
        public List<MockBookDto>? MockSearchResults { get; set; }
        public string? SearchQuery { get; set; }
        public List<Branch> ActiveBranches { get; set; } = new();
        public string DefaultBranchCode { get; set; } = "CEN";

        // Optional post-import summary alert
        public ImportSummaryViewModel? LastSummary { get; set; }
    }

    /// <summary>
    /// Post-import transaction summary report.
    /// </summary>
    public class ImportSummaryViewModel
    {
        public int? LogId { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public int TotalRows { get; set; }
        public DateTime ImportedAt { get; set; }
        public bool HasErrors => ErrorCount > 0 || SkippedCount > 0;
    }

    /// <summary>
    /// Form submission for importing a book directly from the mock metadata search.
    /// </summary>
    public class AddMockBookViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        public string Genre { get; set; } = "Fiction";

        public string? ISBN { get; set; }
        public int? PageCount { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal ReplacementCost { get; set; } = 25.00m;

        [Required(ErrorMessage = "Please select a branch location.")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Library Code is required.")]
        [RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "Library code must be in the format 'XXX-0000' (e.g., BOK-1010).")]
        public string LibraryCode { get; set; } = string.Empty;
    }
}
