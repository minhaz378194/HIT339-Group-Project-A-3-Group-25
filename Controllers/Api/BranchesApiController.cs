using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models.DTOs;
using LibraTrack.Security;

namespace LibraTrack.Controllers.Api
{
    /// <summary>
    /// Read-only public API endpoint for library branch locations.
    /// </summary>
    [ApiController]
    [Route("api/branches")]
    [ApiKeyAuth]
    [EnableRateLimiting("api-limiter")]
    [Produces("application/json")]
    public class BranchesApiController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public BranchesApiController(LibraryDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all public library branch locations and their current inventory counts.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<BranchDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<BranchDto>>> GetBranches()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    BranchCode = b.BranchCode,
                    Address = b.Address,
                    Phone = b.Phone,
                    IsActive = b.IsActive,
                    TotalItemsCount = b.Items.Count
                })
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(branches);
        }
    }
}
