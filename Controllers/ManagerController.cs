using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;

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

        // GET: /Manager or /Manager/Index
        public async Task<IActionResult> Index()
        {
            var allItems = await _context.Items.ToListAsync();
            var allLoans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .ToListAsync();

            var today = DateTime.Today;
            var currentYear = today.Year;

            // 1. Metric Cards
            var totalLoansCount = allLoans.Count;
            var activeBorrowsCount = allLoans.Count(l => l.ReturnDate == null);
            var overdueLoans = allLoans.Where(l => l.ReturnDate == null && l.DueDate.Date < today).ToList();
            var overdueLoansCount = overdueLoans.Count;

            // Fine Revenue This Year (fines where ReturnDate was this year and fine is paid or collected)
            var fineRevenueThisYear = allLoans
                .Where(l => l.ReturnDate.HasValue && l.ReturnDate.Value.Year == currentYear && l.FinePaid && l.FineAmount > 0)
                .Sum(l => l.FineAmount);

            // Total outstanding and damage liability
            var recordedUnpaidFines = allLoans.Where(l => !l.FinePaid && l.FineAmount > 0).Sum(l => l.FineAmount);
            var dynamicActiveOverdueFines = overdueLoans.Sum(l => (today - l.DueDate.Date).Days * DailyFineRate);
            var totalOutstandingFines = recordedUnpaidFines + dynamicActiveOverdueFines;

            var damagedAndDestroyed = allItems
                .Where(i => i.Status == ItemStatus.Damaged || i.Status == ItemStatus.Destroyed)
                .ToList();

            var damageLiability = damagedAndDestroyed.Sum(i => i.ReplacementCost);

            // 2. Status Distribution (Chart 1 - Doughnut)
            var availableCount = allItems.Count(i => i.Status == ItemStatus.Available);
            var borrowedCount = allItems.Count(i => i.Status == ItemStatus.Borrowed);
            var damagedCount = allItems.Count(i => i.Status == ItemStatus.Damaged);
            var destroyedCount = allItems.Count(i => i.Status == ItemStatus.Destroyed);

            // 3. Monthly Loans by Category (Chart 2 - Bar Chart: Jan–Dec of current year)
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

            // 4. Top 5 Most Borrowed Items
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

        // GET: /Manager/Statistics
        public async Task<IActionResult> Statistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var loansQuery = _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                loansQuery = loansQuery.Where(l => l.BorrowDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                loansQuery = loansQuery.Where(l => l.BorrowDate.Date <= toDate.Value.Date);
            }

            var loansInRange = await loansQuery.ToListAsync();

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

            // Stat 4: Overdue Items Table (IsReturned = false AND DueDate < DateTime.Today)
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

            // Stat 5: Monthly Fine Revenue Line Chart (Last 12 Months)
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

            var model = new ManagerStatisticsViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
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

            return View(model);
        }
    }
}
