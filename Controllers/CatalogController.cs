using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    [AllowAnonymous]
    public class CatalogController : Controller
    {
        private readonly LibraryDbContext _context;

        public CatalogController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Catalog or /
        public async Task<IActionResult> Index(string? q, string? type, string? status, string? sort = null, bool available = false, string? genre = null, int? branchId = null)
        {
            var allItemsQuery = _context.Items.Include(i => i.Branch).AsQueryable();

            // Compute global category counts
            var totalBooks = await _context.Books.CountAsync();
            var totalMusic = await _context.MusicAlbums.CountAsync();
            var totalToys = await _context.Toys.CountAsync();

            // Compute global status counts
            var availableCount = await _context.Items.CountAsync(i => i.Status == ItemStatus.Available);
            var borrowedCount = await _context.Items.CountAsync(i => i.Status == ItemStatus.Borrowed);
            var damagedCount = await _context.Items.CountAsync(i => i.Status == ItemStatus.Damaged);
            var destroyedCount = await _context.Items.CountAsync(i => i.Status == ItemStatus.Destroyed);

            var query = allItemsQuery;

            // Type filter
            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals("Book", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OfType<Book>();
                }
                else if (type.Equals("Music", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OfType<Music>();
                }
                else if (type.Equals("Toy", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OfType<Toy>();
                }
            }

            // Status filter (both 'available=true' legacy param and 'status' enum string)
            if (available || (!string.IsNullOrEmpty(status) && status.Equals("Available", StringComparison.OrdinalIgnoreCase)))
            {
                query = query.Where(i => i.Status == ItemStatus.Available);
                status = "Available";
            }
            else if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("Borrowed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Status == ItemStatus.Borrowed);
                }
                else if (status.Equals("Damaged", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Status == ItemStatus.Damaged);
                }
                else if (status.Equals("Destroyed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Status == ItemStatus.Destroyed);
                }
            }

            // Search keyword filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(term) ||
                    i.LibraryCode.ToLower().Contains(term) ||
                    i.Description.ToLower().Contains(term) ||
                    (i is Book && ((Book)i).Author.ToLower().Contains(term)) ||
                    (i is Book && ((Book)i).Genre.ToLower().Contains(term)) ||
                    (i is Music && ((Music)i).Artist.ToLower().Contains(term)) ||
                    (i is Music && ((Music)i).Genre != null && ((Music)i).Genre!.ToLower().Contains(term)) ||
                    (i is Toy && ((Toy)i).ToyType.ToLower().Contains(term)) ||
                    (i is Toy && ((Toy)i).Brand != null && ((Toy)i).Brand!.ToLower().Contains(term))
                );
            }

            // Genre filter (Books)
            if (!string.IsNullOrWhiteSpace(genre))
            {
                var genreTerm = genre.Trim().ToLower();
                query = query.Where(i => i is Book && ((Book)i).Genre.ToLower() == genreTerm);
            }

            // Branch filter
            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(i => i.BranchId == branchId.Value);
            }

            // Sorting
            query = sort switch
            {
                "title_desc" => query.OrderByDescending(i => i.Name),
                "newest" => query.OrderByDescending(i => i.CreatedAt),
                "cost_asc" => query.OrderBy(i => i.ReplacementCost),
                "cost_desc" => query.OrderByDescending(i => i.ReplacementCost),
                _ => query.OrderBy(i => i.Name)
            };

            var results = await query.ToListAsync();

            var genres = await _context.Books
                .Select(b => b.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var viewModel = new CatalogSearchViewModel
            {
                SearchQuery = q,
                SelectedType = type,
                SelectedStatus = status,
                SortBy = sort,
                AvailableOnly = available || (status == "Available"),
                SelectedGenre = genre,
                SelectedBranchId = branchId,
                Branches = branches,
                Results = results,
                TotalResultsCount = results.Count,
                BooksCount = totalBooks,
                MusicCount = totalMusic,
                ToysCount = totalToys,
                AvailableCount = availableCount,
                BorrowedCount = borrowedCount,
                DamagedCount = damagedCount,
                DestroyedCount = destroyedCount,
                AvailableGenres = genres
            };

            return View(viewModel);
        }

        // GET: /Catalog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .Include(i => i.Loans.OrderByDescending(l => l.BorrowDate))
                .ThenInclude(l => l.Borrower)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                TempData["Error"] = "Requested item was not found.";
                return RedirectToAction(nameof(Index));
            }

            // Requirement 6: Show queue length on the item detail page
            ViewBag.WaitingQueueCount = await _context.Reservations
                .CountAsync(r => r.ItemId == id && r.Status == ReservationStatus.Waiting);

            ViewBag.ActiveReadyHold = await _context.Reservations
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.ItemId == id && 
                                          r.Status == ReservationStatus.Ready && 
                                          (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));

            return View(item);
        }
    }
}
