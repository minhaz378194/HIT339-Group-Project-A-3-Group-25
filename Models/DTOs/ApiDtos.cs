namespace LibraTrack.Models.DTOs
{
    /// <summary>
    /// Lightweight DTO representing a catalog item in collection listings.
    /// Excludes internal relational tracking, borrower identities, and staff data.
    /// </summary>
    public class ItemDto
    {
        public int Id { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public decimal ReplacementCost { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Detailed DTO for single item inspection, including category-specific metadata.
    /// Never includes loan transaction records or patron details.
    /// </summary>
    public class ItemDetailDto : ItemDto
    {
        // Book-specific metadata
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? ISBN { get; set; }
        public int? PageCount { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }

        // Music-specific metadata
        public string? Artist { get; set; }
        public string? RecordLabel { get; set; }
        public string? Format { get; set; }
        public int? DurationMinutes { get; set; }
        public int? ReleaseYear { get; set; }

        // Toy-specific metadata
        public string? ToyType { get; set; }
        public int? RecommendedAgeMin { get; set; }
        public int? RecommendedAgeMax { get; set; }
        public string? Brand { get; set; }
        public int? PieceCount { get; set; }
        public string? SafetyNotes { get; set; }
    }

    /// <summary>
    /// DTO for library catalog category classification.
    /// </summary>
    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// DTO representing a library branch location.
    /// </summary>
    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int TotalItemsCount { get; set; }
    }

    /// <summary>
    /// DTO reporting operational health and catalog inventory status.
    /// </summary>
    public class ApiStatusDto
    {
        public string AppName { get; set; } = "LibraTrack";
        public string Version { get; set; } = "1.0.0";
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;
        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int BorrowedItems { get; set; }
        public int DamagedItems { get; set; }
        public int TotalBranches { get; set; }
        public string Status { get; set; } = "Healthy";
    }

    /// <summary>
    /// Generic paginated envelope for collection results.
    /// </summary>
    public class PagedResultDto<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }
}
