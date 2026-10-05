namespace LibraTrack.Services
{
    public class MockBookDto
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = "General";
        public string ISBN { get; set; } = string.Empty;
        public int? PageCount { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal ReplacementCost { get; set; } = 29.99m;
    }

    /// <summary>
    /// Contract for external or simulated book catalog metadata providers.
    /// </summary>
    public interface IBookMetadataProvider
    {
        Task<IEnumerable<MockBookDto>> SearchAsync(string query);
        Task<MockBookDto?> GetByIsbnAsync(string isbn);
    }
}
