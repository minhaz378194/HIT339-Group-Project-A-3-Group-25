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
    /// Read-only public API endpoint for system health, operational version, and inventory totals.
    /// </summary>
    [ApiController]
    [Route("api/status")]
    [ApiKeyAuth]
    [EnableRateLimiting("api-limiter")]
    [Produces("application/json")]
    public class StatusApiController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public StatusApiController(LibraryDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves operational service status, server time, and live catalog totals.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiStatusDto>> GetStatus()
        {
            var totalItems = await _context.Items.CountAsync();
            var availableItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Available);
            var borrowedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Borrowed);
            var damagedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Damaged);
            var totalBranches = await _context.Branches.CountAsync(b => b.IsActive);

            var statusDto = new ApiStatusDto
            {
                AppName = "LibraTrack Public Catalog API",
                Version = "1.0.0",
                ServerTime = DateTime.UtcNow,
                TotalItems = totalItems,
                AvailableItems = availableItems,
                BorrowedItems = borrowedItems,
                DamagedItems = damagedItems,
                TotalBranches = totalBranches,
                Status = "Healthy"
            };

            return Ok(statusDto);
        }
    }
}
