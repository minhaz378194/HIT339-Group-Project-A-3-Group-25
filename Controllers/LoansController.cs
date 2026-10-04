using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;
using LibraTrack.Services;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Reception")]
    public class LoansController : Controller
    {
        private readonly LibraryDbContext _context;
        private const decimal DailyFineRate = 1.00m; // $1.00 per day overdue

        public LoansController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Loans
        public async Task<IActionResult> Index(string? filter = "active", string? search = null)
        {
            var query = _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l =>
                    l.Item.Name.ToLower().Contains(term) ||
                    l.Item.LibraryCode.ToLower().Contains(term) ||
                    l.Borrower.FirstName.ToLower().Contains(term) ||
                    l.Borrower.LastName.ToLower().Contains(term) ||
                    l.Borrower.BorrowerCardNumber.ToLower().Contains(term));
            }

            var now = DateTime.UtcNow;

            switch (filter?.ToLower())
            {
                case "overdue":
                    query = query.Where(l => l.ReturnDate == null && l.DueDate.Date < now.Date);
                    break;
                case "returned":
                    query = query.Where(l => l.ReturnDate != null);
                    break;
                case "all":
                    break;
                case "active":
                default:
                    query = query.Where(l => l.ReturnDate == null);
                    break;
            }

            var loans = await query.OrderByDescending(l => l.BorrowDate).ToListAsync();

            ViewBag.ActiveFilter = filter?.ToLower() ?? "active";
            ViewBag.SearchTerm = search;

            // Recalculate dynamic fines for active overdue items
            foreach (var loan in loans.Where(l => l.ReturnDate == null && l.IsOverdue))
            {
                loan.FineAmount = loan.CalculateDynamicFine(DailyFineRate);
            }

            return View(loans);
        }

        // GET: /Loans/Issue
        public async Task<IActionResult> Issue(int? itemId = null, int? borrowerId = null)
        {
            var availableItems = await _context.Items
                .Where(i => i.Status == ItemStatus.Available)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var activeBorrowers = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName)
                .ToListAsync();

            var model = new IssueLoanViewModel
            {
                ItemId = itemId ?? 0,
                BorrowerId = borrowerId ?? 0,
                BorrowDate = DateTime.UtcNow,
                LoanDays = 14,
                AvailableItems = availableItems,
                ActiveBorrowers = activeBorrowers
            };

            return View(model);
        }

        // POST: /Loans/Issue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(IssueLoanViewModel model)
        {
            var item = await _context.Items.FindAsync(model.ItemId);
            var borrower = await _context.Borrowers.FindAsync(model.BorrowerId);

            if (item == null)
            {
                ModelState.AddModelError("ItemId", "Please select a valid item.");
            }
            else if (item.Status != ItemStatus.Available)
            {
                ModelState.AddModelError("ItemId", $"Item '{item.Name}' is not currently available (Status: {item.Status}).");
            }

            if (borrower == null)
            {
                ModelState.AddModelError("BorrowerId", "Please select a registered borrower.");
            }
            else if (!borrower.IsActive)
            {
                ModelState.AddModelError("BorrowerId", $"Borrower '{borrower.FullName}' is inactive/suspended.");
            }

            if (ModelState.IsValid)
            {
                var loan = new Loan
                {
                    ItemId = model.ItemId,
                    BorrowerId = model.BorrowerId,
                    BorrowDate = model.BorrowDate,
                    DueDate = model.BorrowDate.AddDays(model.LoanDays),
                    ReturnDate = null,
                    FineAmount = 0.00m,
                    FinePaid = false,
                    Notes = model.Notes
                };

                // Update item status to Borrowed
                item!.Status = ItemStatus.Borrowed;

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"'{item.Name}' ({item.LibraryCode}) successfully issued to {borrower!.FullName} until {loan.DueDate:dd MMM yyyy}.";
                return RedirectToAction(nameof(Index), new { filter = "active" });
            }

            // Reload lists if invalid
            model.AvailableItems = await _context.Items
                .Where(i => i.Status == ItemStatus.Available)
                .OrderBy(i => i.Name)
                .ToListAsync();

            model.ActiveBorrowers = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName)
                .ToListAsync();

            return View(model);
        }

        // GET: /Loans/Return/5
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                TempData["Error"] = "Loan record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (loan.ReturnDate != null)
            {
                TempData["Error"] = "This item has already been marked as returned.";
                return RedirectToAction(nameof(Index));
            }

            var returnDate = DateTime.UtcNow;
            var overdueDays = (returnDate.Date - loan.DueDate.Date).Days;
            if (overdueDays < 0) overdueDays = 0;
            var fine = overdueDays * DailyFineRate;

            var model = new ReturnLoanViewModel
            {
                LoanId = loan.Id,
                ItemId = loan.ItemId,
                LibraryCode = loan.Item.LibraryCode,
                ItemTitle = loan.Item.Name,
                ItemType = loan.Item.ItemType,
                BorrowerId = loan.BorrowerId,
                BorrowerName = loan.Borrower.FullName,
                BorrowerCardNumber = loan.Borrower.BorrowerCardNumber,
                BorrowDate = loan.BorrowDate,
                DueDate = loan.DueDate,
                ReturnDate = returnDate,
                OverdueDays = overdueDays,
                FineAmount = fine,
                MarkFineAsPaid = fine > 0, // default to collecting fine
                Condition = ReturnCondition.Good,
                Notes = loan.Notes
            };

            return View(model);
        }

        // POST: /Loans/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(ReturnLoanViewModel model)
        {
            var loan = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == model.LoanId);

            if (loan == null)
            {
                TempData["Error"] = "Loan record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                loan.ReturnDate = model.ReturnDate;
                loan.FineAmount = model.FineAmount;
                loan.FinePaid = model.FineAmount == 0 || model.MarkFineAsPaid;
                loan.ReturnedCondition = model.Condition;
                loan.Notes = string.IsNullOrWhiteSpace(model.Notes) ? loan.Notes : model.Notes;

                // Update physical item status based on returned condition
                switch (model.Condition)
                {
                    case ReturnCondition.Good:
                        loan.Item.Status = ItemStatus.Available;
                        break;
                    case ReturnCondition.Damaged:
                        loan.Item.Status = ItemStatus.Damaged;
                        break;
                    case ReturnCondition.Destroyed:
                        loan.Item.Status = ItemStatus.Destroyed;
                        break;
                }

                await _context.SaveChangesAsync();

                var fineMsg = model.FineAmount > 0 
                    ? (model.MarkFineAsPaid ? $" (Late fine of ${model.FineAmount:F2} paid)." : $" (Late fine of ${model.FineAmount:F2} recorded as UNPAID).") 
                    : "";
                var condMsg = model.Condition != ReturnCondition.Good 
                    ? $" Item status updated to {model.Condition}." 
                    : "";

                TempData["Success"] = $"'{loan.Item.Name}' successfully returned by {loan.Borrower.FullName}.{fineMsg}{condMsg}";
                return RedirectToAction(nameof(Index), new { filter = "returned" });
            }

            return View(model);
        }

        // POST: /Loans/PayFine/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                TempData["Error"] = "Loan record not found.";
                return RedirectToAction(nameof(Index));
            }

            loan.FinePaid = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Fine of ${loan.FineAmount:F2} for borrower '{loan.Borrower.FullName}' marked as PAID.";
            return RedirectToAction(nameof(Index));
        }
    }
}
