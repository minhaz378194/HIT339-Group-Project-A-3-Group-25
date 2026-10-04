using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.DTOs;
using LibraTrack.Security;

namespace LibraTrack.Controllers.Api
{
    /// <summary>
    /// Read-only public API endpoint for catalog items with search, categorization, and pagination.
    /// </summary>
    [ApiController]
    [Route("api/items")]
    [ApiKeyAuth]
    [EnableRateLimiting("api-limiter")]
    [Produces("application/json")]
    public class ItemsApiController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public ItemsApiController(LibraryDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of catalog items with optional filtering.
        /// </summary>
        /// <param name="search">Keyword query matching item name, code, or description.</param>
        /// <param name="category">Filter by discriminator category (Book, Music, Toy).</param>
        /// <param name="status">Filter by physical availability status (Available, Borrowed, Damaged, Destroyed).</param>
        /// <param name="branchCode">Filter by branch code (e.g. CEN, CAS, PAL).</param>
        /// <param name="page">1-based page index (default: 1).</param>
        /// <param name="pageSize">Number of records per page (default: 10, max: 50).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<ItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResultDto<ItemDto>>> GetItems(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] string? branchCode = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Sanitize pagination limits
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Items
                .Include(i => i.Branch)
                .AsNoTracking()
                .AsQueryable();

            // 1. Category / Discriminator Filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim();
                if (cat.Equals("Book", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i is Book);
                }
                else if (cat.Equals("Music", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i is Music);
                }
                else if (cat.Equals("Toy", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i is Toy);
                }
            }

            // 2. Status Filter
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ItemStatus>(status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(i => i.Status == parsedStatus);
            }

            // 3. Branch Code Filter
            if (!string.IsNullOrWhiteSpace(branchCode))
            {
                var code = branchCode.Trim().ToUpperInvariant();
                query = query.Where(i => i.Branch != null && i.Branch.BranchCode.ToUpper() == code);
            }

            // 4. Keyword Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.LibraryCode.ToLower().Contains(term) ||
                    i.Description.ToLower().Contains(term) ||
                    (i is Book && ((Book)i).Author.ToLower().Contains(term)) ||
                    (i is Music && ((Music)i).Artist.ToLower().Contains(term)) ||
                    (i is Toy && ((Toy)i).Brand != null && ((Toy)i).Brand!.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(i => i.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemDto
                {
                    Id = i.Id,
                    LibraryCode = i.LibraryCode,
                    Name = i.Name,
                    ItemType = i.ItemType.ToString(),
                    Description = i.Description,
                    Status = i.Status.ToString(),
                    BranchId = i.BranchId,
                    BranchName = i.Branch != null ? i.Branch.Name : "Main",
                    BranchCode = i.Branch != null ? i.Branch.BranchCode : "CEN",
                    ReplacementCost = i.ReplacementCost,
                    ImageUrl = i.ImageUrl,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResultDto<ItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return Ok(result);
        }

        /// <summary>
        /// Retrieves complete catalog details for a single item by its ID.
        /// Excludes patron and loan transaction information.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ItemDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ItemDetailDto>> GetItemById(int id)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound(new
                {
                    status = 404,
                    error = "Not Found",
                    message = $"Catalog item with ID '{id}' was not found."
                });
            }

            var dto = new ItemDetailDto
            {
                Id = item.Id,
                LibraryCode = item.LibraryCode,
                Name = item.Name,
                ItemType = item.ItemType.ToString(),
                Description = item.Description,
                Status = item.Status.ToString(),
                BranchId = item.BranchId,
                BranchName = item.Branch?.Name ?? "Main",
                BranchCode = item.Branch?.BranchCode ?? "CEN",
                ReplacementCost = item.ReplacementCost,
                ImageUrl = item.ImageUrl,
                CreatedAt = item.CreatedAt
            };

            // Map category-specific polymorphic metadata without revealing internal models
            if (item is Book book)
            {
                dto.Author = book.Author;
                dto.Genre = book.Genre;
                dto.ISBN = book.ISBN;
                dto.PageCount = book.PageCount;
                dto.Publisher = book.Publisher;
                dto.PublicationYear = book.PublicationYear;
            }
            else if (item is Music music)
            {
                dto.Artist = music.Artist;
                dto.RecordLabel = music.RecordLabel;
                dto.Format = music.Format;
                dto.DurationMinutes = music.DurationMinutes;
                dto.Genre = music.Genre;
                dto.ReleaseYear = music.ReleaseYear;
            }
            else if (item is Toy toy)
            {
                dto.ToyType = toy.ToyType;
                dto.RecommendedAgeMin = toy.RecommendedAgeMin;
                dto.RecommendedAgeMax = toy.RecommendedAgeMax;
                dto.Brand = toy.Brand;
                dto.PieceCount = toy.PieceCount;
                dto.SafetyNotes = toy.SafetyNotes;
            }

            return Ok(dto);
        }
    }
}
