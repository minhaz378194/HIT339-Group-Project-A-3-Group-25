using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;
using LibraTrack.Services;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly LibraryDbContext _context;
        private const decimal DailyFineRate = 1.00m;

        public ManagerController(LibraryDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 1. GET: /Manager or /Manager/Index (Dashboard Overview)
        // =========================================================================
        public async Task<IActionResult> Index()
        {
            var allItems = await _context.Items.Include(i => i.Branch).ToListAsync();
            var allLoans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .ToListAsync();

            var today = DateTime.Today;
            var currentYear = today.Year;

            // Metric Cards
            var totalLoansCount = allLoans.Count;
            var activeBorrowsCount = allLoans.Count(l => l.ReturnDate == null);
            var overdueLoans = allLoans.Where(l => l.ReturnDate == null && l.DueDate.Date < today).ToList();
            var overdueLoansCount = overdueLoans.Count;

            var fineRevenueThisYear = allLoans
                .Where(l => l.ReturnDate.HasValue && l.ReturnDate.Value.Year == currentYear && l.FinePaid && l.FineAmount > 0)
                .Sum(l => l.FineAmount);

            var recordedUnpaidFines = allLoans.Where(l => !l.FinePaid && l.FineAmount > 0).Sum(l => l.FineAmount);
            var dynamicActiveOverdueFines = overdueLoans.Sum(l => (today - l.DueDate.Date).Days * DailyFineRate);
            var totalOutstandingFines = recordedUnpaidFines + dynamicActiveOverdueFines;

            var damagedAndDestroyed = allItems
                .Where(i => i.Status == ItemStatus.Damaged || i.Status == ItemStatus.Destroyed)
                .ToList();

            var damageLiability = damagedAndDestroyed.Sum(i => i.ReplacementCost);

            // Status Distribution
            var availableCount = allItems.Count(i => i.Status == ItemStatus.Available);
            var borrowedCount = allItems.Count(i => i.Status == ItemStatus.Borrowed);
            var damagedCount = allItems.Count(i => i.Status == ItemStatus.Damaged);
            var destroyedCount = allItems.Count(i => i.Status == ItemStatus.Destroyed);

            // Monthly Loans
            var monthsLabels = new List<string>();
            var monthlyBooks = new List<int>();
            var monthlyMusic = new List<int>();
            var monthlyToys = new List<int>();

            for (int month = 1; month <= 12; month++)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
                monthsLabels.Add(monthName);

                var loansInMonth = allLoans.Where(l => l.BorrowDate.Year == currentYear && l.BorrowDate.Month == month).ToList();

                monthlyBooks.Add(loansInMonth.Count(l => l.Item is Book));
                monthlyMusic.Add(loansInMonth.Count(l => l.Item is Music));
                monthlyToys.Add(loansInMonth.Count(l => l.Item is Toy));
            }

            // Top 5 Most Borrowed Items
            var topBorrowed = allLoans
                .GroupBy(l => l.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Count = g.Count(),
                    LastBorrowDate = g.Max(l => (DateTime?)l.BorrowDate)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var topBorrowedDtoList = new List<TopBorrowedItemDto>();
            foreach (var tb in topBorrowed)
            {
                var itm = allItems.FirstOrDefault(i => i.Id == tb.ItemId);
                if (itm != null)
                {
                    topBorrowedDtoList.Add(new TopBorrowedItemDto
                    {
                        ItemId = itm.Id,
                        LibraryCode = itm.LibraryCode,
                        Name = itm.Name,
                        ItemType = itm.ItemType,
                        BorrowCount = tb.Count,
                        LastBorrowedDate = tb.LastBorrowDate,
                        CurrentStatus = itm.Status
                    });
                }
            }

            var viewModel = new ManagerDashboardViewModel
            {
                TotalLoansCount = totalLoansCount,
                ActiveBorrowsCount = activeBorrowsCount,
                OverdueLoansCount = overdueLoansCount,
                FineRevenueCollected = fineRevenueThisYear,
                TotalOutstandingFines = totalOutstandingFines,
                DamageLiabilityCost = damageLiability,

                AvailableItemsCount = availableCount,
                BorrowedItemsCount = borrowedCount,
                DamagedItemsCount = damagedCount,
                DestroyedItemsCount = destroyedCount,

                MonthsLabels = monthsLabels,
                MonthlyBookLoans = monthlyBooks,
                MonthlyMusicLoans = monthlyMusic,
                MonthlyToyLoans = monthlyToys,

                TopBorrowedItems = topBorrowedDtoList,
                DamagedAndDestroyedItems = damagedAndDestroyed
            };

            return View(viewModel);
        }

        // =========================================================================
        // 2. REPORT 1: Borrowing Statistics
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> BorrowingStatistics(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildBorrowingStatisticsModelAsync(fromDate, toDate, branchId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportBorrowingStatisticsCsv(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildBorrowingStatisticsModelAsync(fromDate, toDate, branchId);
            var sb = new StringBuilder();
            sb.AppendLine("LoanId,BorrowDate,DueDate,ReturnDate,LibraryCode,ItemName,Category,BranchName,BorrowerName,BorrowerCardNumber,Status");

            foreach (var r in model.Rows)
            {
                var itemTitle = EscapeCsv(r.ItemName);
                var branchName = EscapeCsv(r.BranchName);
                var borrower = EscapeCsv(r.BorrowerName);
                var retDate = r.ReturnDate?.ToString("yyyy-MM-dd") ?? "";

                sb.AppendLine($"{r.LoanId},{r.BorrowDate:yyyy-MM-dd},{r.DueDate:yyyy-MM-dd},{retDate},{r.LibraryCode},{itemTitle},{r.ItemCategory},{branchName},{borrower},{r.BorrowerCardNumber},{r.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Borrowing_Statistics_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBorrowingStatisticsPdf(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildBorrowingStatisticsModelAsync(fromDate, toDate, branchId);
            var pdfBytes = ManagerReportPdfGenerator.GenerateBorrowingStatisticsPdf(model);
            return File(pdfBytes, "application/pdf", $"Borrowing_Statistics_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
        }

        private async Task<BorrowingStatisticsReportViewModel> BuildBorrowingStatisticsModelAsync(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            var query = _context.Loans
                .Include(l => l.Item).ThenInclude(i => i.Branch)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (fromDate.HasValue) query = query.Where(l => l.BorrowDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(l => l.BorrowDate.Date <= toDate.Value.Date);
            if (branchId.HasValue) query = query.Where(l => l.Item.BranchId == branchId.Value);

            var loans = await query.OrderByDescending(l => l.BorrowDate).ToListAsync();
            var branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            var today = DateTime.Today;

            string branchName = "All Branches";
            if (branchId.HasValue)
            {
                var br = branches.FirstOrDefault(b => b.Id == branchId.Value);
                if (br != null) branchName = br.Name;
            }

            var rows = loans.Select(l => new BorrowingReportRow
            {
                LoanId = l.Id,
                BorrowDate = l.BorrowDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                LibraryCode = l.Item.LibraryCode,
                ItemName = l.Item.Name,
                ItemCategory = l.Item.ItemType.ToString(),
                BranchName = l.Item.Branch?.Name ?? "Main Library",
                BorrowerName = l.Borrower.FullName,
                BorrowerCardNumber = l.Borrower.BorrowerCardNumber,
                Status = l.ReturnDate.HasValue ? "Returned" : (l.DueDate.Date < today ? "Overdue" : "Active")
            }).ToList();

            return new BorrowingStatisticsReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedBranchId = branchId,
                SelectedBranchName = branchName,
                Branches = branches,
                Rows = rows
            };
        }

        // =========================================================================
        // 3. REPORT 2: Fine Revenue Audit
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> FineRevenueAudit(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildFineRevenueAuditModelAsync(fromDate, toDate, branchId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportFineRevenueAuditCsv(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildFineRevenueAuditModelAsync(fromDate, toDate, branchId);
            var sb = new StringBuilder();
            sb.AppendLine("LoanId,Date,BorrowerName,BorrowerCardNumber,LibraryCode,ItemName,BranchName,DaysOverdue,FineAmount,PaymentStatus");

            foreach (var r in model.Rows)
            {
                var borrower = EscapeCsv(r.BorrowerName);
                var itemTitle = EscapeCsv(r.ItemName);
                var branchName = EscapeCsv(r.BranchName);

                sb.AppendLine($"{r.LoanId},{r.IncidentDate:yyyy-MM-dd},{borrower},{r.BorrowerCardNumber},{r.LibraryCode},{itemTitle},{branchName},{r.DaysOverdue},{r.FineAmount:F2},{r.PaymentStatus}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Fine_Revenue_Audit_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportFineRevenueAuditPdf(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildFineRevenueAuditModelAsync(fromDate, toDate, branchId);
            var pdfBytes = ManagerReportPdfGenerator.GenerateFineRevenueAuditPdf(model);
            return File(pdfBytes, "application/pdf", $"Fine_Revenue_Audit_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
        }

        private async Task<FineRevenueAuditReportViewModel> BuildFineRevenueAuditModelAsync(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            var query = _context.Loans
                .Include(l => l.Item).ThenInclude(i => i.Branch)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (branchId.HasValue) query = query.Where(l => l.Item.BranchId == branchId.Value);

            var allLoans = await query.ToListAsync();
            var branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            var today = DateTime.Today;

            string branchName = "All Branches";
            if (branchId.HasValue)
            {
                var br = branches.FirstOrDefault(b => b.Id == branchId.Value);
                if (br != null) branchName = br.Name;
            }

            var fineLoans = allLoans
                .Where(l => l.FineAmount > 0 || (l.ReturnDate == null && l.DueDate.Date < today))
                .ToList();

            if (fromDate.HasValue)
                fineLoans = fineLoans.Where(l => (l.ReturnDate ?? l.DueDate).Date >= fromDate.Value.Date).ToList();
            if (toDate.HasValue)
                fineLoans = fineLoans.Where(l => (l.ReturnDate ?? l.DueDate).Date <= toDate.Value.Date).ToList();

            var rows = fineLoans.Select(l =>
            {
                var overdueDays = l.ReturnDate.HasValue
                    ? Math.Max(0, (l.ReturnDate.Value.Date - l.DueDate.Date).Days)
                    : Math.Max(0, (today - l.DueDate.Date).Days);

                var fineAmt = l.FineAmount > 0 ? l.FineAmount : (overdueDays * DailyFineRate);

                return new FineRevenueReportRow
                {
                    LoanId = l.Id,
                    IncidentDate = l.ReturnDate ?? l.DueDate,
                    BorrowerName = l.Borrower.FullName,
                    BorrowerCardNumber = l.Borrower.BorrowerCardNumber,
                    LibraryCode = l.Item.LibraryCode,
                    ItemName = l.Item.Name,
                    BranchName = l.Item.Branch?.Name ?? "Main Library",
                    DaysOverdue = overdueDays,
                    FineAmount = fineAmt,
                    IsPaid = l.FinePaid
                };
            }).OrderByDescending(r => r.IncidentDate).ToList();

            return new FineRevenueAuditReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedBranchId = branchId,
                SelectedBranchName = branchName,
                Branches = branches,
                Rows = rows
            };
        }

        // =========================================================================
        // 4. REPORT 3: Inventory Health
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> InventoryHealth(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildInventoryHealthModelAsync(fromDate, toDate, branchId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryHealthCsv(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildInventoryHealthModelAsync(fromDate, toDate, branchId);
            var sb = new StringBuilder();
            sb.AppendLine("ItemId,LibraryCode,Name,Category,BranchName,Status,ReplacementCost,TimesBorrowed,AddedDate");

            foreach (var r in model.Rows)
            {
                var name = EscapeCsv(r.ItemName);
                var branchName = EscapeCsv(r.BranchName);

                sb.AppendLine($"{r.ItemId},{r.LibraryCode},{name},{r.ItemCategory},{branchName},{r.Status},{r.ReplacementCost:F2},{r.TimesBorrowed},{r.AddedDate:yyyy-MM-dd}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Inventory_Health_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryHealthPdf(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildInventoryHealthModelAsync(fromDate, toDate, branchId);
            var pdfBytes = ManagerReportPdfGenerator.GenerateInventoryHealthPdf(model);
            return File(pdfBytes, "application/pdf", $"Inventory_Health_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
        }

        private async Task<InventoryHealthReportViewModel> BuildInventoryHealthModelAsync(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            var query = _context.Items
                .Include(i => i.Branch)
                .Include(i => i.Loans)
                .AsQueryable();

            if (fromDate.HasValue) query = query.Where(i => i.CreatedAt.Date >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(i => i.CreatedAt.Date <= toDate.Value.Date);
            if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);

            var items = await query.OrderBy(i => i.LibraryCode).ToListAsync();
            var branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

            string branchName = "All Branches";
            if (branchId.HasValue)
            {
                var br = branches.FirstOrDefault(b => b.Id == branchId.Value);
                if (br != null) branchName = br.Name;
            }

            var rows = items.Select(i => new InventoryHealthReportRow
            {
                ItemId = i.Id,
                LibraryCode = i.LibraryCode,
                ItemName = i.Name,
                ItemCategory = i.ItemType.ToString(),
                BranchName = i.Branch?.Name ?? "Main Library",
                Status = i.Status,
                ReplacementCost = i.ReplacementCost,
                TimesBorrowed = i.Loans.Count,
                AddedDate = i.CreatedAt
            }).ToList();

            return new InventoryHealthReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedBranchId = branchId,
                SelectedBranchName = branchName,
                Branches = branches,
                Rows = rows
            };
        }

        // =========================================================================
        // 5. GET: /Manager/Statistics (Executive Summary with CSV/PDF Exports)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Statistics(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildStatisticsViewModelAsync(fromDate, toDate, branchId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportStatisticsCsv(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildStatisticsViewModelAsync(fromDate, toDate, branchId);
            var sb = new StringBuilder();

            sb.AppendLine("=== EXECUTIVE KPI SUMMARY ===");
            sb.AppendLine($"Branch,{EscapeCsv(model.SelectedBranchName)}");
            sb.AppendLine($"Date Range,{model.FromDate?.ToString("yyyy-MM-dd") ?? "All"} to {model.ToDate?.ToString("yyyy-MM-dd") ?? "All"}");
            sb.AppendLine($"Most Popular Item,{EscapeCsv(model.MostPopularItemName ?? "None")} ({model.MostPopularItemBorrowCount} borrows)");
            sb.AppendLine($"Most Active Patron,{EscapeCsv(model.MostActiveBorrowerName ?? "None")} ({model.MostActiveBorrowerCardNumber}) - {model.MostActiveBorrowerCount} borrows");
            sb.AppendLine($"Average Loan Duration (Days),{model.AverageLoanDurationDays}");
            sb.AppendLine($"Total Overdue Items,{model.OverdueItems.Count}");
            sb.AppendLine();

            sb.AppendLine("=== OVERDUE ITEMS AUDIT ===");
            sb.AppendLine("Borrower,ItemTitle,DueDate,DaysOverdue,FineAccruing");
            foreach (var o in model.OverdueItems)
            {
                sb.AppendLine($"{EscapeCsv(o.BorrowerName)},{EscapeCsv(o.ItemName)},{o.DueDate:yyyy-MM-dd},{o.DaysOverdue},{o.FineAccruing:F2}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Manager_Statistics_Summary_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportStatisticsPdf(DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null)
        {
            var model = await BuildStatisticsViewModelAsync(fromDate, toDate, branchId);
            var pdfBytes = ManagerReportPdfGenerator.GenerateStatisticsSummaryPdf(model, model.SelectedBranchName);
            return File(pdfBytes, "application/pdf", $"Manager_Statistics_Summary_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
        }

        private async Task<ManagerStatisticsViewModel> BuildStatisticsViewModelAsync(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            var loansQuery = _context.Loans
                .Include(l => l.Item).ThenInclude(i => i.Branch)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (fromDate.HasValue) loansQuery = loansQuery.Where(l => l.BorrowDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue) loansQuery = loansQuery.Where(l => l.BorrowDate.Date <= toDate.Value.Date);
            if (branchId.HasValue) loansQuery = loansQuery.Where(l => l.Item.BranchId == branchId.Value);

            var loansInRange = await loansQuery.ToListAsync();
            var branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

            string branchName = "All Branches";
            if (branchId.HasValue)
            {
                var br = branches.FirstOrDefault(b => b.Id == branchId.Value);
                if (br != null) branchName = br.Name;
            }

            // Stat 1: Most Popular Item
            var popularGroup = loansInRange
                .GroupBy(l => l.ItemId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            string? popItemName = null;
            ItemType? popItemType = null;
            int popCount = 0;

            if (popularGroup != null)
            {
                var popItem = await _context.Items.FindAsync(popularGroup.Key);
                if (popItem != null)
                {
                    popItemName = popItem.Name;
                    popItemType = popItem.ItemType;
                    popCount = popularGroup.Count();
                }
            }

            // Stat 2: Most Active Borrower
            var activeBorrowerGroup = loansInRange
                .GroupBy(l => l.BorrowerId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            string? activeBorrowerName = null;
            string? activeBorrowerCard = null;
            int activeBorrowerCount = 0;

            if (activeBorrowerGroup != null)
            {
                var brw = await _context.Borrowers.FindAsync(activeBorrowerGroup.Key);
                if (brw != null)
                {
                    activeBorrowerName = brw.FullName;
                    activeBorrowerCard = brw.BorrowerCardNumber;
                    activeBorrowerCount = activeBorrowerGroup.Count();
                }
            }

            // Stat 3: Average Loan Duration
            var returnedLoans = loansInRange.Where(l => l.ReturnDate.HasValue).ToList();
            double avgDuration = 0;
            if (returnedLoans.Any())
            {
                avgDuration = returnedLoans.Average(l => (l.ReturnDate!.Value.Date - l.BorrowDate.Date).TotalDays);
            }

            // Stat 4: Overdue Items
            var today = DateTime.Today;
            var overdueItems = loansInRange
                .Where(l => l.ReturnDate == null && l.DueDate.Date < today)
                .OrderByDescending(l => (today - l.DueDate.Date).Days)
                .Select(l => new OverdueStatDto
                {
                    BorrowerName = l.Borrower.FullName,
                    ItemName = l.Item.Name,
                    DueDate = l.DueDate,
                    DaysOverdue = (today - l.DueDate.Date).Days,
                    FineAccruing = (today - l.DueDate.Date).Days * DailyFineRate
                })
                .ToList();

            // Stat 5: Monthly Fine Revenue
            var fineMonthsLabels = new List<string>();
            var fineMonthlyRevenues = new List<decimal>();

            var allLoans = await _context.Loans.ToListAsync();
            var now = DateTime.UtcNow;

            for (int i = 11; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthLabel = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                fineMonthsLabels.Add(monthLabel);

                var fineInMonth = allLoans
                    .Where(l => l.ReturnDate.HasValue && l.ReturnDate.Value.Year == monthDate.Year && l.ReturnDate.Value.Month == monthDate.Month)
                    .Sum(l => l.FineAmount);

                fineMonthlyRevenues.Add(fineInMonth);
            }

            return new ManagerStatisticsViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedBranchId = branchId,
                SelectedBranchName = branchName,
                Branches = branches,
                MostPopularItemName = popItemName,
                MostPopularItemType = popItemType,
                MostPopularItemBorrowCount = popCount,
                MostActiveBorrowerName = activeBorrowerName,
                MostActiveBorrowerCardNumber = activeBorrowerCard,
                MostActiveBorrowerCount = activeBorrowerCount,
                AverageLoanDurationDays = Math.Round(avgDuration, 1),
                OverdueItems = overdueItems,
                FineMonthsLabels = fineMonthsLabels,
                FineMonthlyRevenues = fineMonthlyRevenues
            };
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
