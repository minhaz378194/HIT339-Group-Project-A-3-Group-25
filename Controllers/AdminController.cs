using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LibraryDbContext _context;

        public AdminController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Admin or /Admin/Index
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

            var allItems = await _context.Items.ToListAsync();

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

            return View("~/Views/Items/Index.cshtml", viewModel);
        }

        // Redirections / Aliases for /Admin/Books/Create, /Admin/Music/Create, /Admin/Toys/Create
        [Route("Admin/Books/Create")]
        public IActionResult CreateBook() => RedirectToAction("CreateBook", "Items");

        [Route("Admin/Music/Create")]
        public IActionResult CreateMusic() => RedirectToAction("CreateMusic", "Items");

        [Route("Admin/Toys/Create")]
        public IActionResult CreateToy() => RedirectToAction("CreateToy", "Items");

        [Route("Admin/Books/Edit/{id}")]
        public IActionResult EditBook(int id) => RedirectToAction("EditBook", "Items", new { id });

        [Route("Admin/Music/Edit/{id}")]
        public IActionResult EditMusic(int id) => RedirectToAction("EditMusic", "Items", new { id });

        [Route("Admin/Toys/Edit/{id}")]
        public IActionResult EditToy(int id) => RedirectToAction("EditToy", "Items", new { id });

        [Route("Admin/Delete/{id}")]
        public IActionResult Delete(int id) => RedirectToAction("Delete", "Items", new { id });
    }
}
