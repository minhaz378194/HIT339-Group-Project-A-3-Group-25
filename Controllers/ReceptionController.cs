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
    public class ReceptionController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IReservationService _reservationService;
        private const decimal DailyFineRate = 1.00m; // $1.00 per day overdue (or $1.50)

        public ReceptionController(
            LibraryDbContext context,
            INotificationService notificationService,
            IReservationService reservationService)
        {
            _context = context;
            _notificationService = notificationService;
            _reservationService = reservationService;
        }

        // GET: /Reception or /Reception/Index
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

            var today = DateTime.Today;
            var activeLoans = allLoans.Where(l => l.ReturnDate == null).ToList();
            var overdueLoans = activeLoans.Where(l => l.DueDate.Date < today).ToList();

            // Calculate live unpaid fines
            var unpaidFinesTotal = allLoans.Where(l => !l.FinePaid && l.FineAmount > 0).Sum(l => l.FineAmount);
            // Add dynamic fines for active overdue loans
            foreach (var ol in overdueLoans)
            {
                var days = (today - ol.DueDate.Date).Days;
                if (days > 0 && ol.FineAmount == 0)
                {
                    unpaidFinesTotal += days * DailyFineRate;
                }
            }

            var viewModel = new ReceptionDashboardViewModel
            {
                TotalBorrowers = await _context.Borrowers.CountAsync(),
                ActiveLoansCount = activeLoans.Count,
                OverdueLoansCount = overdueLoans.Count,
                TotalUnpaidFines = unpaidFinesTotal,
                Borrowers = borrowers,
                RecentLoans = allLoans.Take(10).ToList(),
                OverdueLoans = overdueLoans
            };

            return View(viewModel);
        }

        // GET: /Reception/CreateBorrower
        public async Task<IActionResult> CreateBorrower()
        {
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

        // POST: /Reception/CreateBorrower
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBorrower(Borrower borrower)
        {
            if (await _context.Borrowers.AnyAsync(b => b.BorrowerCardNumber == borrower.BorrowerCardNumber))
            {
                ModelState.AddModelError("BorrowerCardNumber", $"Library Card Number '{borrower.BorrowerCardNumber}' is already in use.");
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

        // GET: /Reception/EditBorrower/5
        public async Task<IActionResult> EditBorrower(int id)
        {
            var borrower = await _context.Borrowers.FindAsync(id);
            if (borrower == null)
            {
                TempData["Error"] = "Borrower not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(borrower);
        }

        // POST: /Reception/EditBorrower/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBorrower(int id, Borrower borrower)
        {
            if (id != borrower.Id) return NotFound();

            if (await _context.Borrowers.AnyAsync(b => b.BorrowerCardNumber == borrower.BorrowerCardNumber && b.Id != id))
            {
                ModelState.AddModelError("BorrowerCardNumber", $"Library Card Number '{borrower.BorrowerCardNumber}' belongs to another patron.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(borrower);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Borrower '{borrower.FullName}' details updated successfully.";
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

        // POST: /Reception/DeleteBorrower/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBorrower(int id)
        {
            var borrower = await _context.Borrowers
                .Include(b => b.Loans)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
            {
                TempData["Error"] = "Borrower record not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if borrower has active loans
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

        // GET: /Reception/IssueItem
        public async Task<IActionResult> IssueItem(string? code = null, int? borrowerId = null)
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

            int selectedItemId = 0;
            if (!string.IsNullOrWhiteSpace(code))
            {
                var match = availableItems.FirstOrDefault(i => i.LibraryCode.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null) selectedItemId = match.Id;
            }

            var model = new ReceptionIssueViewModel
            {
                ItemLibraryCode = code,
                ItemId = selectedItemId,
                BorrowerId = borrowerId ?? 0,
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                AvailableItems = availableItems,
                ActiveBorrowers = activeBorrowers
            };

            return View(model);
        }

        // POST: /Reception/IssueItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueItem(ReceptionIssueViewModel model)
        {
            Item? item = null;

            // 1. Resolve item
            if (model.ItemId > 0)
            {
                item = await _context.Items.FindAsync(model.ItemId);
            }
            else if (!string.IsNullOrWhiteSpace(model.ItemLibraryCode))
            {
                var code = model.ItemLibraryCode.Trim().ToLower();
                item = await _context.Items.FirstOrDefaultAsync(i => i.LibraryCode.ToLower() == code);
            }

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(IssueItem));
            }

            if (item.Status != ItemStatus.Available)
            {
                TempData["Error"] = "This item is not available for borrowing.";
                return RedirectToAction(nameof(IssueItem));
            }

            // 2. Resolve borrower
            Borrower? borrower = null;
            if (model.BorrowerId > 0)
            {
                borrower = await _context.Borrowers.FindAsync(model.BorrowerId);
            }
            else if (!string.IsNullOrWhiteSpace(model.BorrowerQuery))
            {
                var q = model.BorrowerQuery.Trim().ToLower();
                borrower = await _context.Borrowers.FirstOrDefaultAsync(b =>
                    b.BorrowerCardNumber.ToLower() == q ||
                    b.Email.ToLower() == q ||
                    (b.FirstName.ToLower() + " " + b.LastName.ToLower()).Contains(q));
            }

            if (borrower == null)
            {
                TempData["Error"] = "Borrower not found.";
                return RedirectToAction(nameof(IssueItem));
            }

            if (!borrower.IsActive)
            {
                TempData["Error"] = $"Borrower '{borrower.FullName}' account is currently suspended.";
                return RedirectToAction(nameof(IssueItem));
            }

            // Check hold reservation shelf: Do not let other patrons issue an item held for someone else
            var readyReservation = await _reservationService.GetActiveReadyReservationAsync(item.Id);
            if (readyReservation != null && readyReservation.BorrowerId != borrower.Id)
            {
                TempData["Error"] = $"Item '{item.Name}' is held on the Ready shelf for patron {readyReservation.Borrower.FullName} until {readyReservation.ExpiresAt:dd MMM yyyy HH:mm} UTC.";
                return RedirectToAction(nameof(IssueItem));
            }

            // 3. Create Borrow Transaction
            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                BorrowDate = model.BorrowDate,
                DueDate = model.DueDate,
                ReturnDate = null,
                FineAmount = 0.00m,
                FinePaid = false,
                Notes = model.Notes
            };

            // Set item status to Borrowed
            item.Status = ItemStatus.Borrowed;

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            // When the reserved borrower borrows the item, mark reservation Fulfilled and update queue positions
            var patronReservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ItemId == item.Id && 
                                          r.BorrowerId == borrower.Id && 
                                          (r.Status == ReservationStatus.Ready || r.Status == ReservationStatus.Waiting));
            if (patronReservation != null)
            {
                patronReservation.Status = ReservationStatus.Fulfilled;
                patronReservation.QueuePosition = 0;
                await _context.SaveChangesAsync();
                await _reservationService.ResequenceQueueAsync(item.Id);
            }

            // Dispatch simulated notification on issue
            try
            {
                loan.Item = item;
                loan.Borrower = borrower;
                await _notificationService.SendItemIssuedNotificationAsync(loan);
            }
            catch
            {
                // Non-blocking notification dispatch
            }

            TempData["Success"] = $"Item issued successfully to {borrower.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reception/ReturnItem
        public async Task<IActionResult> ReturnItem(string? code = null)
        {
            var activeLoans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .Where(l => l.ReturnDate == null)
                .OrderByDescending(l => l.BorrowDate)
                .ToListAsync();

            var model = new ReceptionReturnViewModel
            {
                LibraryCode = code,
                ActiveLoansList = activeLoans
            };

            if (!string.IsNullOrWhiteSpace(code))
            {
                var term = code.Trim().ToLower();
                var match = activeLoans.FirstOrDefault(l =>
                    l.Item.LibraryCode.ToLower() == term ||
                    l.Item.Name.ToLower().Contains(term));

                if (match != null)
                {
                    var today = DateTime.Today;
                    var overdueDays = (today - match.DueDate.Date).Days;
                    if (overdueDays < 0) overdueDays = 0;
                    var fine = overdueDays * DailyFineRate;

                    model.LoanFound = true;
                    model.LoanId = match.Id;
                    model.ItemTitle = match.Item.Name;
                    model.ItemCode = match.Item.LibraryCode;
                    model.ItemType = match.Item.ItemType;
                    model.BorrowerName = match.Borrower.FullName;
                    model.BorrowerCardNumber = match.Borrower.BorrowerCardNumber;
                    model.BorrowDate = match.BorrowDate;
                    model.DueDate = match.DueDate;
                    model.OverdueDays = overdueDays;
                    model.FineAmount = fine;
                    model.Condition = ReturnCondition.Good;
                    model.MarkFineAsPaid = fine > 0;
                }
                else
                {
                    TempData["Error"] = "No active loan found for this library code.";
                }
            }

            return View(model);
        }

        // POST: /Reception/ConfirmReturn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(ReceptionReturnViewModel model)
        {
            if (!model.LoanId.HasValue)
            {
                TempData["Error"] = "No active loan selected for return.";
                return RedirectToAction(nameof(ReturnItem));
            }

            var loan = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == model.LoanId.Value);

            if (loan == null || loan.ReturnDate != null)
            {
                TempData["Error"] = "Active loan record was not found or has already been returned.";
                return RedirectToAction(nameof(Index));
            }

            var today = DateTime.Today;
            var overdueDays = (today - loan.DueDate.Date).Days;
            if (overdueDays < 0) overdueDays = 0;
            var fine = overdueDays * DailyFineRate;

            loan.ReturnDate = today;
            loan.FineAmount = fine;
            loan.FinePaid = fine == 0 || model.MarkFineAsPaid;
            loan.ReturnedCondition = model.Condition;
            loan.Notes = string.IsNullOrWhiteSpace(model.Notes) ? loan.Notes : model.Notes;

            // Physical item status transition
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

            // Dispatch fine notification if overdue fine was created
            if (fine > 0)
            {
                try
                {
                    await _notificationService.SendFineIssuedNotificationAsync(loan, fine);
                }
                catch
                {
                    // Non-blocking notification dispatch
                }
            }

            // Promote waiting reservation if returned in good condition
            string reservationMsg = "";
            if (model.Condition == ReturnCondition.Good)
            {
                try
                {
                    var promoted = await _reservationService.PromoteNextWaitingReservationAsync(loan.ItemId);
                    if (promoted != null)
                    {
                        reservationMsg = $" Hold active: placed on Ready hold shelf for {promoted.Borrower?.FullName} (Expires {promoted.ExpiresAt:dd MMM yyyy}).";
                    }
                }
                catch
                {
                    // Non-blocking reservation advancement
                }
            }

            if (fine > 0)
            {
                TempData["Success"] = $"Item returned successfully. Fine charged: ${fine:F2}.{reservationMsg}";
            }
            else
            {
                TempData["Success"] = $"Item returned on time. No fine charged.{reservationMsg}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
