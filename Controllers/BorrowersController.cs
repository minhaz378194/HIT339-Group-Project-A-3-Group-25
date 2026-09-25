using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Reception")]
    public class BorrowersController : Controller
    {
        private readonly LibraryDbContext _context;

        public BorrowersController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Borrowers (Reception Dashboard)
        public async Task<IActionResult> Index(string? search = null)
        {
            var query = _context.Borrowers
                .Include(b => b.Loans)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b =>
                    b.FirstName.ToLower().Contains(term) ||
                    b.LastName.ToLower().Contains(term) ||
                    b.BorrowerCardNumber.ToLower().Contains(term) ||
                    b.Email.ToLower().Contains(term) ||
                    b.Phone.Contains(term));
            }

            var borrowers = await query.OrderBy(b => b.LastName).ThenBy(b => b.FirstName).ToListAsync();

            var allLoans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .OrderByDescending(l => l.BorrowDate)
                .ToListAsync();

            var recentLoans = allLoans.Take(10).ToList();
            var overdueLoans = allLoans.Where(l => l.ReturnDate == null && l.DueDate.Date < DateTime.UtcNow.Date).ToList();

            var viewModel = new ReceptionDashboardViewModel
            {
                TotalBorrowers = await _context.Borrowers.CountAsync(),
                ActiveLoansCount = allLoans.Count(l => l.ReturnDate == null),
                OverdueLoansCount = overdueLoans.Count,
                TotalUnpaidFines = allLoans.Where(l => !l.FinePaid && l.FineAmount > 0).Sum(l => l.FineAmount),
                Borrowers = borrowers,
                RecentLoans = recentLoans,
                OverdueLoans = overdueLoans
            };

            return View(viewModel);
        }

        // GET: /Borrowers/Create
        public async Task<IActionResult> Create()
        {
            // Suggest next available card number
            var existingCards = await _context.Borrowers
                .Select(b => b.BorrowerCardNumber)
                .ToListAsync();

            int nextNum = 1001;
            foreach (var card in existingCards)
            {
                if (card != null && card.StartsWith("BRW-") && int.TryParse(card.Substring(4), out int n))
                {
                    if (n >= nextNum) nextNum = n + 1;
                }
            }

            var suggestedCode = $"BRW-{nextNum}";

            return View(new Borrower { BorrowerCardNumber = suggestedCode, IsActive = true, MembershipDate = DateTime.UtcNow });
        }

        // POST: /Borrowers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Borrower borrower)
        {
            if (await _context.Borrowers.AnyAsync(b => b.BorrowerCardNumber == borrower.BorrowerCardNumber))
            {
                ModelState.AddModelError("BorrowerCardNumber", $"Card number '{borrower.BorrowerCardNumber}' is already registered.");
            }

            if (ModelState.IsValid)
            {
                borrower.MembershipDate = DateTime.UtcNow;
                _context.Borrowers.Add(borrower);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Borrower '{borrower.FullName}' ({borrower.BorrowerCardNumber}) registered successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(borrower);
        }

        // GET: /Borrowers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var borrower = await _context.Borrowers.FindAsync(id);
            if (borrower == null)
            {
                TempData["Error"] = "Borrower not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(borrower);
        }

        // POST: /Borrowers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Borrower borrower)
        {
            if (id != borrower.Id) return NotFound();

            if (await _context.Borrowers.AnyAsync(b => b.BorrowerCardNumber == borrower.BorrowerCardNumber && b.Id != id))
            {
                ModelState.AddModelError("BorrowerCardNumber", $"Card number '{borrower.BorrowerCardNumber}' is already registered to another patron.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(borrower);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Borrower '{borrower.FullName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Borrowers.AnyAsync(e => e.Id == id)) return NotFound();
                    throw;
                }
            }
            return View(borrower);
        }

        // GET: /Borrowers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var borrower = await _context.Borrowers
                .Include(b => b.Loans)
                .ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
            {
                TempData["Error"] = "Borrower not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(borrower);
        }

        // POST: /Borrowers/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var borrower = await _context.Borrowers.FindAsync(id);
            if (borrower == null) return NotFound();

            borrower.IsActive = !borrower.IsActive;
            await _context.SaveChangesAsync();

            var statusStr = borrower.IsActive ? "Activated" : "Suspended";
            TempData["Success"] = $"Borrower '{borrower.FullName}' status changed to {statusStr}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Borrowers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var borrower = await _context.Borrowers
                .Include(b => b.Loans)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
            {
                TempData["Error"] = "Borrower record not found.";
                return RedirectToAction(nameof(Index));
            }

            var hasActiveLoans = borrower.Loans.Any(l => l.ReturnDate == null);
            if (hasActiveLoans)
            {
                TempData["Error"] = "Cannot delete borrower with active loans.";
                return RedirectToAction(nameof(Index));
            }

            _context.Borrowers.Remove(borrower);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Borrower '{borrower.FullName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
