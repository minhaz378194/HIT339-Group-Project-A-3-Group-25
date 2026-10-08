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
    /// Staff controller for placing, monitoring, fulfilling, and cancelling patron item hold reservations.
    /// Accessible to Reception and Admin staff.
    /// </summary>
    [Authorize(Roles = "Reception,Admin")]
    public class ReservationsController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IReservationService _reservationService;
        private readonly INotificationService _notificationService;

        public ReservationsController(
            LibraryDbContext context, 
            IReservationService reservationService,
            INotificationService notificationService)
        {
            _context = context;
            _reservationService = reservationService;
            _notificationService = notificationService;
        }

        // GET: /Reservations
        public async Task<IActionResult> Index(string? filter = "all", string? search = null, int? itemId = null)
        {
            var baseQuery = _context.Reservations
                .Include(r => r.Borrower)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Branch)
                .AsQueryable();

            // Calculate KPI counts
            var totalCount = await _context.Reservations.CountAsync();
            var waitingCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Waiting);
            var readyCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Ready);
            var fulfilledCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Fulfilled);
            var cancelledCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled);
            var expiredCount = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Expired);

            // Filter by item if requested
            if (itemId.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.ItemId == itemId.Value);
            }

            // Filter by active tab
            switch (filter?.ToLower())
            {
                case "waiting":
                    baseQuery = baseQuery.Where(r => r.Status == ReservationStatus.Waiting);
                    break;
                case "ready":
                    baseQuery = baseQuery.Where(r => r.Status == ReservationStatus.Ready);
                    break;
                case "fulfilled":
                    baseQuery = baseQuery.Where(r => r.Status == ReservationStatus.Fulfilled);
                    break;
                case "cancelled":
                    baseQuery = baseQuery.Where(r => r.Status == ReservationStatus.Cancelled);
                    break;
                case "expired":
                    baseQuery = baseQuery.Where(r => r.Status == ReservationStatus.Expired);
                    break;
                default:
                    filter = "all";
                    break;
            }

            // Text search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                baseQuery = baseQuery.Where(r =>
                    r.Item.Name.ToLower().Contains(term) ||
                    r.Item.LibraryCode.ToLower().Contains(term) ||
                    r.Borrower.FirstName.ToLower().Contains(term) ||
                    r.Borrower.LastName.ToLower().Contains(term) ||
                    r.Borrower.BorrowerCardNumber.ToLower().Contains(term) ||
                    r.Borrower.Email.ToLower().Contains(term));
            }

            // Priority ordering: Ready holds first, then Waiting queue position ascending, then recency
            var reservations = await baseQuery
                .OrderBy(r => r.Status == ReservationStatus.Ready ? 0 : (r.Status == ReservationStatus.Waiting ? 1 : 2))
                .ThenBy(r => r.QueuePosition)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new ReservationListViewModel
            {
                Reservations = reservations,
                ActiveFilter = filter,
                SearchTerm = search,
                FilterItemId = itemId,
                TotalCount = totalCount,
                WaitingCount = waitingCount,
                ReadyCount = readyCount,
                FulfilledCount = fulfilledCount,
                CancelledCount = cancelledCount,
                ExpiredCount = expiredCount
            };

            return View(viewModel);
        }

        // GET: /Reservations/Create
        public async Task<IActionResult> Create(int? itemId = null, int? borrowerId = null)
        {
            // Requirement 2: Allow a reservation only when item status is Borrowed or Damaged
            var eligibleItems = await _context.Items
                .Include(i => i.Branch)
                .Where(i => i.Status == ItemStatus.Borrowed || i.Status == ItemStatus.Damaged)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var activeBorrowers = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName)
                .ToListAsync();

            var model = new CreateReservationViewModel
            {
                ItemId = itemId ?? 0,
                BorrowerId = borrowerId ?? 0,
                EligibleItems = eligibleItems,
                ActiveBorrowers = activeBorrowers
            };

            if (itemId.HasValue)
            {
                var selectedItem = eligibleItems.FirstOrDefault(i => i.Id == itemId.Value)
                                   ?? await _context.Items.FindAsync(itemId.Value);

                if (selectedItem != null)
                {
                    model.SelectedItemName = selectedItem.Name;
                    model.SelectedItemCode = selectedItem.LibraryCode;
                    model.SelectedItemStatus = selectedItem.Status;
                    model.CurrentQueueLength = await _reservationService.GetWaitingQueueCountAsync(selectedItem.Id);
                }
            }

            return View(model);
        }

        // POST: /Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReservationViewModel model)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.Id == model.ItemId);

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == model.BorrowerId);

            // Requirement 2: Allow a reservation ONLY when item status is Borrowed or Damaged
            if (item == null)
            {
                ModelState.AddModelError("ItemId", "Please select a valid catalog item.");
            }
            else if (item.Status != ItemStatus.Borrowed && item.Status != ItemStatus.Damaged)
            {
                ModelState.AddModelError("ItemId", $"Reservations can only be placed for items currently Borrowed or Damaged (Current status: {item.Status}). Available items can be checked out directly.");
            }

            if (borrower == null)
            {
                ModelState.AddModelError("BorrowerId", "Please select a registered patron.");
            }
            else if (!borrower.IsActive)
            {
                ModelState.AddModelError("BorrowerId", $"Patron '{borrower.FullName}' has an inactive/suspended membership.");
            }

            // Requirement 2: Block duplicate reservations by the same borrower for the same item
            if (item != null && borrower != null)
            {
                var existingActiveReservation = await _context.Reservations
                    .AnyAsync(r => r.ItemId == model.ItemId &&
                                   r.BorrowerId == model.BorrowerId &&
                                   (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready));

                if (existingActiveReservation)
                {
                    ModelState.AddModelError("BorrowerId", $"Patron '{borrower.FullName}' already holds an active reservation for '{item.Name}'. Duplicate reservations are not permitted.");
                }
            }

            if (ModelState.IsValid)
            {
                // Calculate next queue position
                var currentMax = await _context.Reservations
                    .Where(r => r.ItemId == model.ItemId && r.Status == ReservationStatus.Waiting)
                    .MaxAsync(r => (int?)r.QueuePosition) ?? 0;

                var newQueuePos = currentMax + 1;

                var reservation = new Reservation
                {
                    ItemId = model.ItemId,
                    BorrowerId = model.BorrowerId,
                    QueuePosition = newQueuePos,
                    Status = ReservationStatus.Waiting,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Hold reservation placed successfully for {borrower!.FullName} on '{item!.Name}'. Queue position: #{newQueuePos}.";
                return RedirectToAction(nameof(Index), new { filter = "waiting" });
            }

            // Re-populate lists if validation failed
            model.EligibleItems = await _context.Items
                .Include(i => i.Branch)
                .Where(i => i.Status == ItemStatus.Borrowed || i.Status == ItemStatus.Damaged)
                .OrderBy(i => i.Name)
                .ToListAsync();

            model.ActiveBorrowers = await _context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName)
                .ToListAsync();

            return View(model);
        }

        // POST: /Reservations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Borrower)
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                TempData["Error"] = "Reservation record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (reservation.Status != ReservationStatus.Waiting && reservation.Status != ReservationStatus.Ready)
            {
                TempData["Error"] = $"Cannot cancel reservation with status '{reservation.Status}'.";
                return RedirectToAction(nameof(Index));
            }

            var previousStatus = reservation.Status;
            var itemId = reservation.ItemId;
            var patronName = reservation.Borrower.FullName;

            reservation.Status = ReservationStatus.Cancelled;
            reservation.QueuePosition = 0;
            await _context.SaveChangesAsync();

            // If cancelled from Ready, promote the next waiting patron immediately
            if (previousStatus == ReservationStatus.Ready)
            {
                var nextPromoted = await _reservationService.PromoteNextWaitingReservationAsync(itemId);
                if (nextPromoted != null)
                {
                    TempData["Info"] = $"Hold for {patronName} cancelled. Next patron {nextPromoted.Borrower?.FullName} promoted to Ready!";
                }
            }
            else
            {
                // Re-sequence remaining waiting queue
                await _reservationService.ResequenceQueueAsync(itemId);
            }

            TempData["Success"] = $"Reservation #{id} for '{patronName}' was cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
