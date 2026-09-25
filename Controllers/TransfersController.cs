using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Reception,Admin")]
    public class TransfersController : Controller
    {
        private readonly LibraryDbContext _context;

        public TransfersController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /Transfers
        public async Task<IActionResult> Index(string? status = "all", string? search = null)
        {
            var query = _context.ItemTransfers
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Include(t => t.RequestedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Item.Name.ToLower().Contains(term) ||
                    t.Item.LibraryCode.ToLower().Contains(term) ||
                    t.FromBranch.Name.ToLower().Contains(term) ||
                    t.ToBranch.Name.ToLower().Contains(term) ||
                    (t.Notes != null && t.Notes.ToLower().Contains(term)));
            }

            var allTransfers = await _context.ItemTransfers.ToListAsync();
            var totalCount = allTransfers.Count;
            var requestedCount = allTransfers.Count(t => t.Status == TransferStatus.Requested);
            var inTransitCount = allTransfers.Count(t => t.Status == TransferStatus.InTransit);
            var receivedCount = allTransfers.Count(t => t.Status == TransferStatus.Received);

            switch (status?.ToLower())
            {
                case "requested":
                    query = query.Where(t => t.Status == TransferStatus.Requested);
                    break;
                case "intransit":
                    query = query.Where(t => t.Status == TransferStatus.InTransit);
                    break;
                case "received":
                    query = query.Where(t => t.Status == TransferStatus.Received);
                    break;
                case "cancelled":
                    query = query.Where(t => t.Status == TransferStatus.Cancelled);
                    break;
                case "all":
                default:
                    break;
            }

            var transfers = await query
                .OrderByDescending(t => t.RequestedAt)
                .ToListAsync();

            var viewModel = new TransferListViewModel
            {
                Transfers = transfers,
                ActiveFilter = status?.ToLower() ?? "all",
                SearchTerm = search,
                TotalCount = totalCount,
                RequestedCount = requestedCount,
                InTransitCount = inTransitCount,
                ReceivedCount = receivedCount
            };

            return View(viewModel);
        }

        // GET: /Transfers/Create
        public async Task<IActionResult> Create(int? itemId = null)
        {
            var availableItems = await _context.Items
                .Include(i => i.Branch)
                .Where(i => i.Status != ItemStatus.Borrowed)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var model = new CreateTransferViewModel
            {
                AvailableItems = availableItems,
                DestinationBranches = branches
            };

            if (itemId.HasValue)
            {
                var selectedItem = availableItems.FirstOrDefault(i => i.Id == itemId.Value);
                if (selectedItem != null)
                {
                    model.ItemId = selectedItem.Id;
                    model.FromBranchId = selectedItem.BranchId;
                    model.FromBranchName = selectedItem.Branch?.Name;
                }
            }

            return View(model);
        }

        // POST: /Transfers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTransferViewModel model)
        {
            var item = await _context.Items
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.Id == model.ItemId);

            if (item == null)
            {
                ModelState.AddModelError("ItemId", "Please select a valid item to transfer.");
            }
            else
            {
                // Requirement 6: Block transfers for items that are currently borrowed
                if (item.Status == ItemStatus.Borrowed)
                {
                    ModelState.AddModelError("ItemId", $"Item '{item.Name}' is currently borrowed and cannot be transferred until returned.");
                }

                // Check for duplicate active transfer
                var activeTransferExists = await _context.ItemTransfers.AnyAsync(t =>
                    t.ItemId == model.ItemId &&
                    (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit));

                if (activeTransferExists)
                {
                    ModelState.AddModelError("ItemId", $"Item '{item.Name}' already has an active transfer in progress.");
                }

                // Validate destination branch
                if (model.ToBranchId == item.BranchId)
                {
                    ModelState.AddModelError("ToBranchId", "Destination branch must be different from the item's current branch.");
                }
            }

            var toBranch = await _context.Branches.FindAsync(model.ToBranchId);
            if (toBranch == null || !toBranch.IsActive)
            {
                ModelState.AddModelError("ToBranchId", "Please select an active destination branch.");
            }

            if (ModelState.IsValid && item != null && toBranch != null)
            {
                int? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }

                var transfer = new ItemTransfer
                {
                    ItemId = item.Id,
                    FromBranchId = item.BranchId,
                    ToBranchId = toBranch.Id,
                    RequestedByUserId = userId,
                    RequestedAt = DateTime.UtcNow,
                    Status = TransferStatus.Requested,
                    Notes = model.Notes
                };

                _context.ItemTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Transfer request created for '{item.Name}' from {item.Branch.Name} to {toBranch.Name}.";
                return RedirectToAction(nameof(Index));
            }

            // Reload lists on validation error
            model.AvailableItems = await _context.Items
                .Include(i => i.Branch)
                .Where(i => i.Status != ItemStatus.Borrowed)
                .OrderBy(i => i.Name)
                .ToListAsync();

            model.DestinationBranches = await _context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            if (item != null)
            {
                model.FromBranchId = item.BranchId;
                model.FromBranchName = item.Branch?.Name;
            }

            return View(model);
        }

        // POST: /Transfers/MarkInTransit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInTransit(int id)
        {
            var transfer = await _context.ItemTransfers
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer record was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (transfer.Status != TransferStatus.Requested)
            {
                TempData["Error"] = "Only requested transfers can be marked as In Transit.";
                return RedirectToAction(nameof(Index));
            }

            transfer.Status = TransferStatus.InTransit;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Transfer #{transfer.Id} for '{transfer.Item.Name}' is now In Transit to {transfer.ToBranch.Name}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Transfers/MarkReceived/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReceived(int id)
        {
            var transfer = await _context.ItemTransfers
                .Include(t => t.Item)
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer record was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (transfer.Status != TransferStatus.InTransit && transfer.Status != TransferStatus.Requested)
            {
                TempData["Error"] = "This transfer cannot be marked as Received in its current state.";
                return RedirectToAction(nameof(Index));
            }

            // Update transfer record
            transfer.Status = TransferStatus.Received;
            transfer.CompletedAt = DateTime.UtcNow;

            // Requirement 6: This changes the item's BranchId to the destination branch
            transfer.Item.BranchId = transfer.ToBranchId;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Item '{transfer.Item.Name}' successfully received at {transfer.ToBranch.Name}. Item location updated!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Transfers/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var transfer = await _context.ItemTransfers
                .Include(t => t.Item)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer record was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (transfer.Status == TransferStatus.Received || transfer.Status == TransferStatus.Cancelled)
            {
                TempData["Error"] = "Completed or already cancelled transfers cannot be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            transfer.Status = TransferStatus.Cancelled;
            transfer.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Transfer #{transfer.Id} for '{transfer.Item.Name}' has been cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
