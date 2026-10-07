using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItemsController : Controller
    {
        private readonly LibraryDbContext _context;

        public ItemsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Items (Admin Dashboard)
        public async Task<IActionResult> Index(string? tab = "books", string? search = null)
        {
            var booksQuery = _context.Books.AsQueryable();
            var musicQuery = _context.MusicAlbums.AsQueryable();
            var toysQuery = _context.Toys.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                booksQuery = booksQuery.Where(b => b.Name.ToLower().Contains(term) || b.Author.ToLower().Contains(term) || b.LibraryCode.ToLower().Contains(term));
                musicQuery = musicQuery.Where(m => m.Name.ToLower().Contains(term) || m.Artist.ToLower().Contains(term) || m.LibraryCode.ToLower().Contains(term));
                toysQuery = toysQuery.Where(t => t.Name.ToLower().Contains(term) || t.ToyType.ToLower().Contains(term) || t.LibraryCode.ToLower().Contains(term));
            }

            var books = await booksQuery.OrderBy(b => b.LibraryCode).ToListAsync();
            var musicList = await musicQuery.OrderBy(m => m.LibraryCode).ToListAsync();
            var toys = await toysQuery.OrderBy(t => t.LibraryCode).ToListAsync();

            var allItems = await _context.Items.Include(i => i.Branch).ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalBooks = allItems.Count(i => i is Book),
                TotalMusic = allItems.Count(i => i is Music),
                TotalToys = allItems.Count(i => i is Toy),
                TotalItems = allItems.Count,
                AvailableCount = allItems.Count(i => i.Status == ItemStatus.Available),
                BorrowedCount = allItems.Count(i => i.Status == ItemStatus.Borrowed),
                DamagedCount = allItems.Count(i => i.Status == ItemStatus.Damaged),
                DestroyedCount = allItems.Count(i => i.Status == ItemStatus.Destroyed),
                Books = books,
                MusicList = musicList,
                Toys = toys,
                ActiveTab = tab?.ToLower() ?? "books",
                SearchTerm = search
            };

            return View(viewModel);
        }

        // ==========================================
        // BOOK CRUD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreateBook()
        {
            await PopulateBranchesViewBagAsync();
            return View(new Book { Status = ItemStatus.Available, ReplacementCost = 25.00m, BranchId = await GetDefaultBranchIdAsync() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(Book book)
        {
            if (book.BranchId <= 0)
            {
                book.BranchId = await GetDefaultBranchIdAsync();
            }

            if (await _context.Items.AnyAsync(i => i.LibraryCode == book.LibraryCode))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{book.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                book.CreatedAt = DateTime.UtcNow;
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Book '{book.Name}' ({book.LibraryCode}) created successfully.";
                return RedirectToAction(nameof(Index), new { tab = "books" });
            }

            await PopulateBranchesViewBagAsync(book.BranchId);
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction(nameof(Index), new { tab = "books" });
            }
            await PopulateBranchesViewBagAsync(book.BranchId);
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id, Book book)
        {
            if (id != book.Id) return NotFound();

            if (book.BranchId <= 0)
            {
                book.BranchId = await GetDefaultBranchIdAsync();
            }

            if (await _context.Items.AnyAsync(i => i.LibraryCode == book.LibraryCode && i.Id != id))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{book.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Book '{book.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index), new { tab = "books" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Books.AnyAsync(e => e.Id == id)) return NotFound();
                    throw;
                }
            }
            await PopulateBranchesViewBagAsync(book.BranchId);
            return View(book);
        }

        // ==========================================
        // MUSIC CRUD
        // ==========================================
        [HttpGet]
        public IActionResult CreateMusic()
        {
            return View(new Music { Status = ItemStatus.Available, ReleaseYear = DateTime.UtcNow.Year, ReplacementCost = 30.00m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMusic(Music music)
        {
            if (await _context.Items.AnyAsync(i => i.LibraryCode == music.LibraryCode))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{music.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                music.CreatedAt = DateTime.UtcNow;
                _context.MusicAlbums.Add(music);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Music album '{music.Name}' ({music.LibraryCode}) created successfully.";
                return RedirectToAction(nameof(Index), new { tab = "music" });
            }

            return View(music);
        }

        [HttpGet]
        public async Task<IActionResult> EditMusic(int id)
        {
            var music = await _context.MusicAlbums.FindAsync(id);
            if (music == null)
            {
                TempData["Error"] = "Music album not found.";
                return RedirectToAction(nameof(Index), new { tab = "music" });
            }
            return View(music);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMusic(int id, Music music)
        {
            if (id != music.Id) return NotFound();

            if (await _context.Items.AnyAsync(i => i.LibraryCode == music.LibraryCode && i.Id != id))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{music.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(music);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Music album '{music.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index), new { tab = "music" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.MusicAlbums.AnyAsync(e => e.Id == id)) return NotFound();
                    throw;
                }
            }
            return View(music);
        }

        // ==========================================
        // TOY CRUD
        // ==========================================
        [HttpGet]
        public IActionResult CreateToy()
        {
            return View(new Toy { Status = ItemStatus.Available, RecommendedAgeMin = 3, ReplacementCost = 40.00m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateToy(Toy toy)
        {
            if (await _context.Items.AnyAsync(i => i.LibraryCode == toy.LibraryCode))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{toy.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                toy.CreatedAt = DateTime.UtcNow;
                _context.Toys.Add(toy);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Toy '{toy.Name}' ({toy.LibraryCode}) created successfully.";
                return RedirectToAction(nameof(Index), new { tab = "toys" });
            }

            return View(toy);
        }

        [HttpGet]
        public async Task<IActionResult> EditToy(int id)
        {
            var toy = await _context.Toys.FindAsync(id);
            if (toy == null)
            {
                TempData["Error"] = "Toy not found.";
                return RedirectToAction(nameof(Index), new { tab = "toys" });
            }
            return View(toy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditToy(int id, Toy toy)
        {
            if (id != toy.Id) return NotFound();

            if (await _context.Items.AnyAsync(i => i.LibraryCode == toy.LibraryCode && i.Id != id))
            {
                ModelState.AddModelError("LibraryCode", $"Library code '{toy.LibraryCode}' is already in use by another item.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(toy);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Toy '{toy.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index), new { tab = "toys" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Toys.AnyAsync(e => e.Id == id)) return NotFound();
                    throw;
                }
            }
            return View(toy);
        }

        // ==========================================
        // ITEM DETAILS & DELETE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .Include(i => i.Loans.OrderByDescending(l => l.BorrowDate))
                .ThenInclude(l => l.Borrower)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .Include(i => i.Loans)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items
                .Include(i => i.Loans)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if currently on loan
            if (item.Status == ItemStatus.Borrowed || item.Loans.Any(l => l.ReturnDate == null))
            {
                TempData["Error"] = "Cannot delete an item that is currently borrowed.";
                return RedirectToAction(nameof(Index));
            }

            // Remove associated loan history if any or cascade
            if (item.Loans.Any())
            {
                _context.Loans.RemoveRange(item.Loans);
            }

            var type = item.ItemType switch
            {
                ItemType.Book => "books",
                ItemType.Music => "music",
                ItemType.Toy => "toys",
                _ => "books"
            };

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Item '{item.Name}' ({item.LibraryCode}) was deleted from the library system.";
            return RedirectToAction(nameof(Index), new { tab = type });
        }

        // Quick status updater
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ItemStatus status, string? tab = "books")
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Index), new { tab });
            }

            if (item.Status == ItemStatus.Borrowed && status != ItemStatus.Borrowed)
            {
                TempData["Error"] = "Item is currently borrowed. Please process the return through Reception to update its status.";
                return RedirectToAction(nameof(Index), new { tab });
            }

            item.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Status for '{item.Name}' updated to {status}.";
            return RedirectToAction(nameof(Index), new { tab });
        }
    }
}
