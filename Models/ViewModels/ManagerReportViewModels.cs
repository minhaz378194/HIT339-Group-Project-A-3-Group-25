using LibraTrack.Models;

namespace LibraTrack.Models.ViewModels
{
    // =========================================================================
    // 1. Borrowing Statistics Report Models
    // =========================================================================
    public class BorrowingReportRow
    {
        public int LoanId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerCardNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class BorrowingStatisticsReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public string SelectedBranchName { get; set; } = "All Branches";
        public List<Branch> Branches { get; set; } = new();

        public List<BorrowingReportRow> Rows { get; set; } = new();

        // Totals
        public int TotalLoans => Rows.Count;
        public int ActiveLoans => Rows.Count(r => r.ReturnDate == null);
        public int ReturnedLoans => Rows.Count(r => r.ReturnDate != null);
        public int OverdueLoans => Rows.Count(r => r.Status == "Overdue");
        public int UniqueBorrowers => Rows.Select(r => r.BorrowerCardNumber).Distinct().Count();
    }

    // =========================================================================
    // 2. Fine Revenue Audit Report Models
    // =========================================================================
    public class FineRevenueReportRow
    {
        public int LoanId { get; set; }
        public DateTime IncidentDate { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerCardNumber { get; set; } = string.Empty;
        public string LibraryCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentStatus => IsPaid ? "Paid" : "Outstanding";
    }

    public class FineRevenueAuditReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public string SelectedBranchName { get; set; } = "All Branches";
        public List<Branch> Branches { get; set; } = new();

        public List<FineRevenueReportRow> Rows { get; set; } = new();

        // Totals
        public int TotalFinesCount => Rows.Count;
        public decimal TotalFinesAssessed => Rows.Sum(r => r.FineAmount);
        public decimal TotalFinesCollected => Rows.Where(r => r.IsPaid).Sum(r => r.FineAmount);
        public decimal TotalFinesOutstanding => Rows.Where(r => !r.IsPaid).Sum(r => r.FineAmount);
        public int PaidCount => Rows.Count(r => r.IsPaid);
        public int UnpaidCount => Rows.Count(r => !r.IsPaid);
    }

    // =========================================================================
    // 3. Inventory Health Report Models
    // =========================================================================
    public class InventoryHealthReportRow
    {
        public int ItemId { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public ItemStatus Status { get; set; }
        public decimal ReplacementCost { get; set; }
        public int TimesBorrowed { get; set; }
        public DateTime AddedDate { get; set; }
    }

    public class InventoryHealthReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public string SelectedBranchName { get; set; } = "All Branches";
        public List<Branch> Branches { get; set; } = new();

        public List<InventoryHealthReportRow> Rows { get; set; } = new();

        // Totals
        public int TotalItemsCount => Rows.Count;
        public int AvailableCount => Rows.Count(r => r.Status == ItemStatus.Available);
        public int BorrowedCount => Rows.Count(r => r.Status == ItemStatus.Borrowed);
        public int DamagedCount => Rows.Count(r => r.Status == ItemStatus.Damaged);
        public int DestroyedCount => Rows.Count(r => r.Status == ItemStatus.Destroyed);
        public decimal TotalAssetValue => Rows.Sum(r => r.ReplacementCost);
        public decimal DamageLiabilityCost => Rows.Where(r => r.Status == ItemStatus.Damaged || r.Status == ItemStatus.Destroyed).Sum(r => r.ReplacementCost);
    }
}
