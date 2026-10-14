using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;
using LibraTrack.Services;

namespace LibraTrack.Controllers
{
    /// <summary>
    /// Public self-service kiosk controller for patron item checkout and account inspection.
    /// Touch-optimized, high-contrast UI with barcode scanner input support and 60-second auto-timeout.
    /// </summary>
    [AllowAnonymous]
    public class KioskController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IReservationService _reservationService;
        private const decimal DailyFineRate = 1.00m;

        public KioskController(
            LibraryDbContext context,
            INotificationService notificationService,
            IReservationService reservationService)
        {
            _context = context;
            _notificationService = notificationService;
            _reservationService = reservationService;
        }

        // ==========================================
        // 1. KIOSK HOME SCREEN (Requirement 1)
        // ==========================================
        // GET: /Kiosk
        public IActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 2. PATRON IDENTIFICATION & PIN (Requirement 2)
        // ==========================================
        // GET: /Kiosk/Login
        public IActionResult Login(string target = "checkout")
        {
            // If already authenticated in kiosk session, redirect straight to target
            var currentBorrowerId = HttpContext.Session.GetInt32("KioskBorrowerId");
            if (currentBorrowerId.HasValue)
            {
                return target == "account" 
                    ? RedirectToAction(nameof(Account)) 
                    : RedirectToAction(nameof(CheckOut));
            }

            var wrongTries = HttpContext.Session.GetInt32("KioskWrongTries") ?? 0;
            var isLocked = (HttpContext.Session.GetInt32("KioskLocked") ?? 0) == 1;

            var model = new KioskLoginViewModel
            {
                TargetAction = target,
                RemainingAttempts = Math.Max(0, 3 - wrongTries),
                IsLocked = isLocked,
                ErrorMessage = isLocked 
                    ? "Kiosk session locked after 3 unsuccessful attempts. Please see reception staff for assistance." 
                    : null
            };

            return View(model);
        }

        // POST: /Kiosk/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(KioskLoginViewModel model)
        {
            // Check session lock status (Requirement 2)
            var sessionLocked = (HttpContext.Session.GetInt32("KioskLocked") ?? 0) == 1;
            if (sessionLocked)
            {
                model.IsLocked = true;
                model.RemainingAttempts = 0;
                model.ErrorMessage = "Kiosk session locked after 3 unsuccessful attempts. Please see reception staff for assistance.";
                return View(model);
            }

            var wrongTries = HttpContext.Session.GetInt32("KioskWrongTries") ?? 0;

            if (!ModelState.IsValid)
            {
                model.RemainingAttempts = Math.Max(0, 3 - wrongTries);
                return View(model);
            }

            var cardNumber = model.BorrowerCardNumber?.Trim().ToUpperInvariant() ?? string.Empty;
            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.BorrowerCardNumber.ToUpper() == cardNumber);

            // Handle borrower not found
            if (borrower == null)
            {
                wrongTries++;
                HttpContext.Session.SetInt32("KioskWrongTries", wrongTries);

                if (wrongTries >= 3)
                {
                    HttpContext.Session.SetInt32("KioskLocked", 1);
                    model.IsLocked = true;
                    model.RemainingAttempts = 0;
                    model.ErrorMessage = "Kiosk session locked after 3 unsuccessful attempts. Please see reception staff for assistance.";
                }
                else
                {
                    model.RemainingAttempts = 3 - wrongTries;
                    model.ErrorMessage = $"Card number '{cardNumber}' was not found. {model.RemainingAttempts} attempt(s) remaining.";
                }
                return View(model);
            }

            // Check if borrower account itself is locked
            if (borrower.IsLockedOut || !borrower.IsActive)
            {
                model.IsLocked = true;
                model.RemainingAttempts = 0;
                model.ErrorMessage = borrower.IsLockedOut
                    ? "This library card is locked out due to previous failed attempts. Please contact reception staff to reset your PIN."
                    : "This library card membership is inactive or suspended. Please see staff at the front desk.";
                return View(model);
            }

            // Verify 4-digit PIN hash (Requirement 2)
            bool isPinValid = borrower.VerifyPin(model.Pin);

            if (!isPinValid)
            {
                borrower.FailedPinAttempts++;
                wrongTries++;
                HttpContext.Session.SetInt32("KioskWrongTries", wrongTries);

                if (borrower.FailedPinAttempts >= 3 || wrongTries >= 3)
                {
                    borrower.IsLockedOut = true;
                    HttpContext.Session.SetInt32("KioskLocked", 1);
                    await _context.SaveChangesAsync();

                    model.IsLocked = true;
                    model.RemainingAttempts = 0;
                    model.ErrorMessage = "Kiosk session locked after 3 wrong tries. Please contact library staff at reception.";
                }
                else
                {
                    await _context.SaveChangesAsync();
                    model.RemainingAttempts = 3 - wrongTries;
                    model.ErrorMessage = $"Incorrect 4-digit PIN. {model.RemainingAttempts} attempt(s) remaining.";
                }
                return View(model);
            }

            // PIN is correct: reset counters and establish kiosk session
            borrower.FailedPinAttempts = 0;
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("KioskWrongTries");
            HttpContext.Session.Remove("KioskLocked");
            HttpContext.Session.SetInt32("KioskBorrowerId", borrower.Id);
            HttpContext.Session.SetString("KioskBorrowerName", borrower.FullName);
            HttpContext.Session.SetString("KioskBorrowerCard", borrower.BorrowerCardNumber);

            if (model.TargetAction?.ToLower() == "account")
            {
                return RedirectToAction(nameof(Account));
            }

            return RedirectToAction(nameof(CheckOut));
        }

        // ==========================================
        // 3. CHECK OUT SCREEN (Requirements 3 & 6)
        // ==========================================
        // GET: /Kiosk/CheckOut
        public async Task<IActionResult> CheckOut(string? code = null)
        {
            var borrowerId = HttpContext.Session.GetInt32("KioskBorrowerId");
            if (!borrowerId.HasValue)
            {
                return RedirectToAction(nameof(Login), new { target = "checkout" });
            }

            var borrower = await _context.Borrowers.FindAsync(borrowerId.Value);
            if (borrower == null || !borrower.IsActive)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Index));
            }

            var model = new KioskCheckOutViewModel
            {
                Borrower = borrower,
                LibraryCode = code?.Trim().ToUpperInvariant()
            };

            // If a library code was scanned or entered (Barcode Scanner Enter support)
            if (!string.IsNullOrWhiteSpace(model.LibraryCode))
            {
                var item = await _context.Items
                    .Include(i => i.Branch)
                    .FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == model.LibraryCode);

                if (item == null)
                {
                    model.ErrorMessage = $"Item with library code '{model.LibraryCode}' was not found in the library catalog. Please verify the code or scan again.";
                }
                else
                {
                    model.ScannedItem = item;

                    // Validate physical availability
                    if (item.Status != ItemStatus.Available)
                    {
                        model.ErrorMessage = $"Item '{item.Name}' cannot be checked out because its status is currently '{item.Status}'.";
                    }
                    else
                    {
                        // Requirement 4: Check if item is held under a Ready reservation for someone else
                        var readyReservation = await _reservationService.GetActiveReadyReservationAsync(item.Id);
                        if (readyReservation != null && readyReservation.BorrowerId != borrowerId.Value)
                        {
                            model.HoldRestrictionMessage = $"This item is currently on hold shelf for another patron ({readyReservation.Borrower.FullName}) until {readyReservation.ExpiresAt:dd MMM yyyy HH:mm} UTC. Only the reserving patron may check it out.";
                            model.CanConfirm = false;
                        }
                        else
                        {
                            model.CanConfirm = true;
                        }
                    }
                }
            }

            return View(model);
        }

        // POST: /Kiosk/CheckOut
        // Handles barcode scanner submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOut(KioskCheckOutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LibraryCode))
            {
                return RedirectToAction(nameof(CheckOut));
            }

            return RedirectToAction(nameof(CheckOut), new { code = model.LibraryCode.Trim() });
        }

        // POST: /Kiosk/ConfirmCheckOut
        // Reuses existing issue logic & notification service (Requirement 3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckOut(int itemId)
        {
            var borrowerId = HttpContext.Session.GetInt32("KioskBorrowerId");
            if (!borrowerId.HasValue)
            {
                return RedirectToAction(nameof(Login), new { target = "checkout" });
            }

            var borrower = await _context.Borrowers.FindAsync(borrowerId.Value);
            var item = await _context.Items.Include(i => i.Branch).FirstOrDefaultAsync(i => i.Id == itemId);

            if (borrower == null || item == null)
            {
                TempData["KioskError"] = "Could not process checkout. Item or borrower record not found.";
                return RedirectToAction(nameof(CheckOut));
            }

            // Verify item availability
            if (item.Status != ItemStatus.Available)
            {
                TempData["KioskError"] = $"Item '{item.Name}' is not available for loan (Current status: {item.Status}).";
                return RedirectToAction(nameof(CheckOut));
            }

            // Requirement 4: Verify ready reservation hold shelf
            var readyReservation = await _reservationService.GetActiveReadyReservationAsync(item.Id);
            if (readyReservation != null && readyReservation.BorrowerId != borrower.Id)
            {
                TempData["KioskError"] = $"Item '{item.Name}' is reserved for another patron on the hold shelf.";
                return RedirectToAction(nameof(CheckOut));
            }

            // 1. Create standard 14-day loan record
            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                ReturnDate = null,
                FineAmount = 0.00m,
                FinePaid = false,
                Notes = "Public Self-Service Kiosk Checkout"
            };

            // 2. Transition item status to Borrowed
            item.Status = ItemStatus.Borrowed;
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            // 3. Requirement 5: When reserved borrower borrows the item, fulfill reservation
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

            // 4. Reuse existing notification service (Requirement 3)
            try
            {
                loan.Item = item;
                loan.Borrower = borrower;
                await _notificationService.SendItemIssuedNotificationAsync(loan);
            }
            catch
            {
                // Graceful fallback for simulation
            }

            return RedirectToAction(nameof(CheckOutSuccess), new { loanId = loan.Id });
        }

        // GET: /Kiosk/CheckOutSuccess
        public async Task<IActionResult> CheckOutSuccess(int loanId)
        {
            var borrowerId = HttpContext.Session.GetInt32("KioskBorrowerId");
            if (!borrowerId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var loan = await _context.Loans
                .Include(l => l.Item)
                    .ThenInclude(i => i.Branch)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == loanId && l.BorrowerId == borrowerId.Value);

            if (loan == null)
            {
                return RedirectToAction(nameof(CheckOut));
            }

            return View(loan);
        }

        // ==========================================
        // 4. MY ACCOUNT OVERVIEW (Requirement 4)
        // ==========================================
        // GET: /Kiosk/Account
        public async Task<IActionResult> Account()
        {
            var borrowerId = HttpContext.Session.GetInt32("KioskBorrowerId");
            if (!borrowerId.HasValue)
            {
                return RedirectToAction(nameof(Login), new { target = "account" });
            }

            var borrower = await _context.Borrowers.FindAsync(borrowerId.Value);
            if (borrower == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Index));
            }

            // Active (unreturned) loans
            var activeLoans = await _context.Loans
                .Include(l => l.Item)
                    .ThenInclude(i => i.Branch)
                .Where(l => l.BorrowerId == borrower.Id && l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            // Calculate dynamic fines for active overdue items
            foreach (var loan in activeLoans.Where(l => l.IsOverdue))
            {
                loan.FineAmount = loan.CalculateDynamicFine(DailyFineRate);
            }

            // Total unpaid fines (across both active and returned loans)
            var unpaidFinesTotal = await _context.Loans
                .Where(l => l.BorrowerId == borrower.Id && !l.FinePaid && l.FineAmount > 0)
                .SumAsync(l => (decimal?)l.FineAmount) ?? 0.00m;

            // Active reservations (Waiting and Ready)
            var reservations = await _context.Reservations
                .Include(r => r.Item)
                    .ThenInclude(i => i.Branch)
                .Where(r => r.BorrowerId == borrower.Id && 
                            (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.Status == ReservationStatus.Ready ? 0 : 1)
                .ThenBy(r => r.QueuePosition)
                .ToListAsync();

            var viewModel = new KioskAccountViewModel
            {
                Borrower = borrower,
                ActiveLoans = activeLoans,
                TotalFinesOwed = unpaidFinesTotal,
                Reservations = reservations
            };

            return View(viewModel);
        }

        // ==========================================
        // 5. FINISH & AUTO-LOGOUT (Requirement 5)
        // ==========================================
        // GET: /Kiosk/Finish
        public IActionResult Finish()
        {
            HttpContext.Session.Clear();
            TempData["KioskMessage"] = "Session completed. Thank you for visiting your library today!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Kiosk/Timeout
        public IActionResult Timeout()
        {
            HttpContext.Session.Clear();
            TempData["KioskAlert"] = "Your kiosk session ended automatically after 60 seconds of inactivity.";
            return RedirectToAction(nameof(Index));
        }
    }
}
