using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models.DTOs;
using LibraTrack.Security;

namespace LibraTrack.Controllers.Api
{
    /// <summary>
    /// Read-only public API endpoint for catalog categories.
    /// </summary>
    [ApiController]
    [Route("api/categories")]
    [ApiKeyAuth]
    [EnableRateLimiting("api-limiter")]
    [Produces("application/json")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public CategoriesApiController(LibraryDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all catalog categories with item inventory counts.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            var booksCount = await _context.Books.CountAsync();
            var musicCount = await _context.MusicAlbums.CountAsync();
            var toysCount = await _context.Toys.CountAsync();

            var categories = new List<CategoryDto>
            {
                new CategoryDto
                {
                    Name = "Book",
                    Description = "Physical print literature, novels, references, and non-fiction texts.",
                    ItemCount = booksCount
                },
                new CategoryDto
                {
                    Name = "Music",
                    Description = "Audio media including vinyl LP records, audio CDs, and classical collections.",
                    ItemCount = musicCount
                },
                new CategoryDto
                {
                    Name = "Toy",
                    Description = "Educational STEM kits, strategy board games, puzzles, and robotics.",
                    ItemCount = toysCount
                }
            };

            return Ok(categories);
        }
    }
}
