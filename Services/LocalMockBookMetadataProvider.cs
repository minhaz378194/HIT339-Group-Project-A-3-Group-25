using System.Text.Json;

namespace LibraTrack.Services
{
    /// <summary>
    /// Local mock implementation of IBookMetadataProvider that reads catalog metadata from MockBooks.json.
    /// </summary>
    public class LocalMockBookMetadataProvider : IBookMetadataProvider
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalMockBookMetadataProvider> _logger;
        private List<MockBookDto>? _cachedBooks;
        private readonly object _lock = new();

        public LocalMockBookMetadataProvider(IWebHostEnvironment env, ILogger<LocalMockBookMetadataProvider> logger)
        {
            _env = env;
            _logger = logger;
        }

        private List<MockBookDto> LoadBooks()
        {
            if (_cachedBooks != null)
            {
                return _cachedBooks;
            }

            lock (_lock)
            {
                if (_cachedBooks != null)
                {
                    return _cachedBooks;
                }

                // Check possible paths for MockBooks.json
                var pathsToTry = new[]
                {
                    Path.Combine(_env.ContentRootPath, "Data", "MockBooks.json"),
                    Path.Combine(_env.ContentRootPath, "MockBooks.json"),
                    Path.Combine(AppContext.BaseDirectory, "Data", "MockBooks.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "MockBooks.json")
                };

                string? resolvedPath = pathsToTry.FirstOrDefault(File.Exists);

                if (resolvedPath != null)
                {
                    try
                    {
                        var json = File.ReadAllText(resolvedPath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        _cachedBooks = JsonSerializer.Deserialize<List<MockBookDto>>(json, options) ?? new List<MockBookDto>();
                        _logger.LogInformation("Loaded {Count} mock books from {Path}", _cachedBooks.Count, resolvedPath);
                        return _cachedBooks;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse MockBooks.json from {Path}", resolvedPath);
                    }
                }
                else
                {
                    _logger.LogWarning("MockBooks.json not found in searched paths.");
                }

                _cachedBooks = new List<MockBookDto>();
                return _cachedBooks;
            }
        }

        public Task<IEnumerable<MockBookDto>> SearchAsync(string query)
        {
            var books = LoadBooks();

            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult<IEnumerable<MockBookDto>>(books.Take(10));
            }

            var cleanQuery = query.Trim().ToLowerInvariant();
            var normalizedIsbnQuery = cleanQuery.Replace("-", "").Replace(" ", "");

            var results = books.Where(b =>
                b.Title.ToLowerInvariant().Contains(cleanQuery) ||
                b.Author.ToLowerInvariant().Contains(cleanQuery) ||
                (b.Genre != null && b.Genre.ToLowerInvariant().Contains(cleanQuery)) ||
                (!string.IsNullOrWhiteSpace(b.ISBN) && b.ISBN.Replace("-", "").Replace(" ", "").Contains(normalizedIsbnQuery))
            ).ToList();

            return Task.FromResult<IEnumerable<MockBookDto>>(results);
        }

        public Task<MockBookDto?> GetByIsbnAsync(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return Task.FromResult<MockBookDto?>(null);
            }

            var clean = isbn.Trim().Replace("-", "").Replace(" ", "").ToLowerInvariant();
            var books = LoadBooks();

            var book = books.FirstOrDefault(b =>
                !string.IsNullOrWhiteSpace(b.ISBN) &&
                b.ISBN.Replace("-", "").Replace(" ", "").ToLowerInvariant() == clean
            );

            return Task.FromResult(book);
        }
    }
}
