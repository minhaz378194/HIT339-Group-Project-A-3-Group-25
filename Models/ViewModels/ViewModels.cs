using System.ComponentModel.DataAnnotations;

namespace LibraTrack.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or Email is required.")]
        [Display(Name = "Username / Email")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMusic { get; set; }
        public int TotalToys { get; set; }
        public int TotalItems { get; set; }

        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int DamagedCount { get; set; }
        public int DestroyedCount { get; set; }

        public List<Book> Books { get; set; } = new();
        public List<Music> MusicList { get; set; } = new();
        public List<Toy> Toys { get; set; } = new();

        public string ActiveTab { get; set; } = "books";
        public string? SearchTerm { get; set; }
    }

    public class ReceptionDashboardViewModel
    {
        public int TotalBorrowers { get; set; }
        public int ActiveLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public decimal TotalUnpaidFines { get; set; }

        public List<Borrower> Borrowers { get; set; } = new();
        public List<Loan> RecentLoans { get; set; } = new();
        public List<Loan> OverdueLoans { get; set; } = new();
    }

    public class ManagerDashboardViewModel
    {
        // Metric cards
        public int TotalLoansCount { get; set; }
        public int ActiveBorrowsCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public decimal FineRevenueCollected { get; set; }
        public decimal TotalOutstandingFines { get; set; }
        public decimal DamageLiabilityCost { get; set; }

        // Item Status Distribution (for Doughnut Chart)
        public int AvailableItemsCount { get; set; }
        public int BorrowedItemsCount { get; set; }
        public int DamagedItemsCount { get; set; }
        public int DestroyedItemsCount { get; set; }

        // Monthly Category Loans (for Bar Chart)
        public List<string> MonthsLabels { get; set; } = new();
        public List<int> MonthlyBookLoans { get; set; } = new();
        public List<int> MonthlyMusicLoans { get; set; } = new();
        public List<int> MonthlyToyLoans { get; set; } = new();

        // Top 5 Most Borrowed Items
        public List<TopBorrowedItemDto> TopBorrowedItems { get; set; } = new();

        // Damaged & Destroyed Items Audit Table
        public List<Item> DamagedAndDestroyedItems { get; set; } = new();
    }

    public class TopBorrowedItemDto
    {
        public int ItemId { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ItemType ItemType { get; set; }
        public int BorrowCount { get; set; }
        public DateTime? LastBorrowedDate { get; set; }
        public ItemStatus CurrentStatus { get; set; }
    }

    public class ManagerStatisticsViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public string SelectedBranchName { get; set; } = "All Branches";
        public List<Branch> Branches { get; set; } = new();

        // Stat 1: Most Popular Item
        public string? MostPopularItemName { get; set; }
        public ItemType? MostPopularItemType { get; set; }
        public int MostPopularItemBorrowCount { get; set; }

        // Stat 2: Most Active Borrower
        public string? MostActiveBorrowerName { get; set; }
        public string? MostActiveBorrowerCardNumber { get; set; }
        public int MostActiveBorrowerCount { get; set; }

        // Stat 3: Average Loan Duration
        public double AverageLoanDurationDays { get; set; }

        // Stat 4: Overdue Items Table
        public List<OverdueStatDto> OverdueItems { get; set; } = new();

        // Stat 5: Monthly Fine Revenue Line Chart (last 12 months)
        public List<string> FineMonthsLabels { get; set; } = new();
        public List<decimal> FineMonthlyRevenues { get; set; } = new();
    }

    public class OverdueStatDto
    {
        public string BorrowerName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public decimal FineAccruing { get; set; }
    }

    public class IssueLoanViewModel
    {
        [Required(ErrorMessage = "Please select an item to issue.")]
        [Display(Name = "Item (Library Code / Title)")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Please select a registered borrower.")]
        [Display(Name = "Borrower (Card # / Name)")]
        public int BorrowerId { get; set; }

        [Required]
        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Loan Duration (Days)")]
        [Range(1, 90, ErrorMessage = "Loan duration must be between 1 and 90 days.")]
        public int LoanDays { get; set; } = 14;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate => BorrowDate.AddDays(LoanDays);

        [StringLength(300)]
        [Display(Name = "Staff Notes")]
        public string? Notes { get; set; }

        public List<Item> AvailableItems { get; set; } = new();
        public List<Borrower> ActiveBorrowers { get; set; } = new();
    }

    public class ReturnLoanViewModel
    {
        [Required]
        public int LoanId { get; set; }

        public int ItemId { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string ItemTitle { get; set; } = string.Empty;
        public ItemType ItemType { get; set; }

        public int BorrowerId { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerCardNumber { get; set; } = string.Empty;

        [Display(Name = "Borrowed Date")]
        public DateTime BorrowDate { get; set; }

        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Overdue Days")]
        public int OverdueDays { get; set; }

        [Display(Name = "Calculated Fine ($)")]
        [Range(0, 5000)]
        public decimal FineAmount { get; set; }

        [Display(Name = "Collect Fine Immediately?")]
        public bool MarkFineAsPaid { get; set; } = true;

        [Required]
        [Display(Name = "Item Condition at Return")]
        public ReturnCondition Condition { get; set; } = ReturnCondition.Good;

        [StringLength(300)]
        [Display(Name = "Return / Condition Notes")]
        public string? Notes { get; set; }
    }

    public class ReceptionIssueViewModel
    {
        [Display(Name = "Borrower Card Number or Search")]
        public string? BorrowerQuery { get; set; }
        public int BorrowerId { get; set; }

        [Display(Name = "Item Library Code or Search")]
        public string? ItemLibraryCode { get; set; }
        public int ItemId { get; set; }

        [Required]
        [Display(Name = "Borrow Date")]
        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        [StringLength(300)]
        [Display(Name = "Staff Notes")]
        public string? Notes { get; set; }

        public List<Item> AvailableItems { get; set; } = new();
        public List<Borrower> ActiveBorrowers { get; set; } = new();
    }

    public class ReceptionReturnViewModel
    {
        [Display(Name = "Enter Library Code")]
        public string? LibraryCode { get; set; }

        public bool LoanFound { get; set; }
        public int? LoanId { get; set; }
        public string? ItemTitle { get; set; }
        public string? ItemCode { get; set; }
        public ItemType? ItemType { get; set; }
        public string? BorrowerName { get; set; }
        public string? BorrowerCardNumber { get; set; }
        public DateTime? BorrowDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal FineAmount { get; set; }

        [Display(Name = "Item Condition")]
        public ReturnCondition Condition { get; set; } = ReturnCondition.Good;

        [Display(Name = "Collect / Mark Fine as Paid")]
        public bool MarkFineAsPaid { get; set; } = true;

        [StringLength(300)]
        [Display(Name = "Return Notes")]
        public string? Notes { get; set; }

        public List<Loan> ActiveLoansList { get; set; } = new();
    }

    public class CatalogSearchViewModel
    {
        public string? SearchQuery { get; set; }
        public string? SelectedType { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SortBy { get; set; }
        public bool AvailableOnly { get; set; }
        public string? SelectedGenre { get; set; }

        public List<Item> Results { get; set; } = new();
        public int TotalResultsCount { get; set; }
        public int BooksCount { get; set; }
        public int MusicCount { get; set; }
        public int ToysCount { get; set; }
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int DamagedCount { get; set; }
        public int DestroyedCount { get; set; }
        public int? SelectedBranchId { get; set; }
        public List<Branch> Branches { get; set; } = new();
        public List<string> AvailableGenres { get; set; } = new();
    }

    public class CreateTransferViewModel
    {
        [Required(ErrorMessage = "Please select an item to transfer.")]
        [Display(Name = "Select Item")]
        public int ItemId { get; set; }

        [Display(Name = "Current Branch")]
        public int FromBranchId { get; set; }
        public string? FromBranchName { get; set; }

        [Required(ErrorMessage = "Please select a destination branch.")]
        [Display(Name = "Destination Branch")]
        public int ToBranchId { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Transfer Notes / Reason")]
        public string? Notes { get; set; }

        public List<Item> AvailableItems { get; set; } = new();
        public List<Branch> DestinationBranches { get; set; } = new();
    }

    public class TransferListViewModel
    {
        public List<ItemTransfer> Transfers { get; set; } = new();
        public string ActiveFilter { get; set; } = "all";
        public string? SearchTerm { get; set; }
        public int TotalCount { get; set; }
        public int RequestedCount { get; set; }
        public int InTransitCount { get; set; }
        public int ReceivedCount { get; set; }
    }

    public class NotificationDashboardViewModel
    {
        public List<Notification> Notifications { get; set; } = new();
        public NotificationType? SelectedType { get; set; }
        public NotificationChannel? SelectedChannel { get; set; }
        public string? SearchTerm { get; set; }

        // Metrics
        public int TotalSentCount { get; set; }
        public int EmailCount { get; set; }
        public int SmsCount { get; set; }

        public int BorrowedCount { get; set; }
        public int DueSoonCount { get; set; }
        public int OverdueCount { get; set; }
        public int FineIssuedCount { get; set; }
        public int ReservationReadyCount { get; set; }
    }

    public class CreateReservationViewModel
    {
        [Required(ErrorMessage = "Please select an item to reserve.")]
        [Display(Name = "Catalog Item")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Please select a patron for the reservation.")]
        [Display(Name = "Borrower / Patron")]
        public int BorrowerId { get; set; }

        [StringLength(300)]
        [Display(Name = "Staff Notes")]
        public string? Notes { get; set; }

        public string? SelectedItemName { get; set; }
        public string? SelectedItemCode { get; set; }
        public ItemStatus? SelectedItemStatus { get; set; }
        public int CurrentQueueLength { get; set; }

        public List<Item> EligibleItems { get; set; } = new();
        public List<Borrower> ActiveBorrowers { get; set; } = new();
    }

    public class ReservationListViewModel
    {
        public List<Reservation> Reservations { get; set; } = new();
        public string ActiveFilter { get; set; } = "all";
        public string? SearchTerm { get; set; }
        public int? FilterItemId { get; set; }

        // KPI Counts
        public int TotalCount { get; set; }
        public int WaitingCount { get; set; }
        public int ReadyCount { get; set; }
        public int FulfilledCount { get; set; }
        public int CancelledCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}
