using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LibraTrack.Models.ViewModels;

namespace LibraTrack.Services
{
    public static class ManagerReportPdfGenerator
    {
        private static readonly string PrimaryColor = "#1E3A8A"; // Deep navy
        private static readonly string SecondaryColor = "#0D9488"; // Teal
        private static readonly string BackgroundLight = "#F8FAFC";
        private static readonly string BorderColor = "#E2E8F0";

        // =========================================================================
        // 1. Borrowing Statistics PDF
        // =========================================================================
        public static byte[] GenerateBorrowingStatisticsPdf(BorrowingStatisticsReportViewModel model)
        {
            var dateRangeText = GetDateRangeText(model.FromDate, model.ToDate);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("LibraTrack Management Analytics").FontSize(10).SemiBold().FontColor(Color.FromHex(SecondaryColor));
                                titleCol.Item().Text("Borrowing Statistics Report").FontSize(18).Bold().FontColor(Color.FromHex(PrimaryColor));
                                titleCol.Item().Text($"Date Range: {dateRangeText}  •  Branch: {model.SelectedBranchName}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(200).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text("AUDIT REPORT").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                metaCol.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8);
                                metaCol.Item().Text("Role: Manager").FontSize(8);
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Color.FromHex(BorderColor));

                        // Summary Metric Cards
                        col.Item().PaddingBottom(12).Row(summaryRow =>
                        {
                            summaryRow.Spacing(10);
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "TOTAL LOANS", model.TotalLoans.ToString(), Colors.Blue.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "ACTIVE BORROWS", model.ActiveLoans.ToString(), Colors.Teal.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "RETURNED", model.ReturnedLoans.ToString(), Colors.Green.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "OVERDUE", model.OverdueLoans.ToString(), Colors.Red.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "PATRONS", model.UniqueBorrowers.ToString(), Colors.Purple.Darken2));
                        });
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(45);  // Loan ID
                            cols.ConstantColumn(75);  // Borrow Date
                            cols.ConstantColumn(75);  // Due Date
                            cols.ConstantColumn(75);  // Return Date
                            cols.ConstantColumn(70);  // Code
                            cols.RelativeColumn(3);   // Title
                            cols.ConstantColumn(60);  // Category
                            cols.RelativeColumn(2);   // Branch
                            cols.RelativeColumn(2);   // Borrower
                            cols.ConstantColumn(60);  // Status
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("# Loan");
                            header.Cell().Element(HeaderCellStyle).Text("Borrowed");
                            header.Cell().Element(HeaderCellStyle).Text("Due Date");
                            header.Cell().Element(HeaderCellStyle).Text("Returned");
                            header.Cell().Element(HeaderCellStyle).Text("Item Code");
                            header.Cell().Element(HeaderCellStyle).Text("Item Title");
                            header.Cell().Element(HeaderCellStyle).Text("Category");
                            header.Cell().Element(HeaderCellStyle).Text("Branch");
                            header.Cell().Element(HeaderCellStyle).Text("Borrower");
                            header.Cell().Element(HeaderCellStyle).Text("Status");
                        });

                        int idx = 0;
                        foreach (var row in model.Rows)
                        {
                            var bg = idx % 2 == 0 ? Colors.White : Color.FromHex(BackgroundLight);
                            idx++;

                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text($"#{row.LoanId}");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.BorrowDate.ToString("yyyy-MM-dd"));
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.DueDate.ToString("yyyy-MM-dd"));
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ReturnDate?.ToString("yyyy-MM-dd") ?? "—");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.LibraryCode).Bold();
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ItemName);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ItemCategory);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.BranchName);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text($"{row.BorrowerName} ({row.BorrowerCardNumber})");
                            
                            // Status with color
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(text =>
                            {
                                if (row.Status == "Overdue")
                                    text.Span(row.Status).Bold().FontColor(Colors.Red.Darken2);
                                else if (row.Status == "Returned")
                                    text.Span(row.Status).FontColor(Colors.Green.Darken2);
                                else
                                    text.Span(row.Status).FontColor(Colors.Blue.Darken2);
                            });
                        }
                    });

                    // Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("LibraTrack Library Management System  •  Manager Confidential").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        // =========================================================================
        // 2. Fine Revenue Audit PDF
        // =========================================================================
        public static byte[] GenerateFineRevenueAuditPdf(FineRevenueAuditReportViewModel model)
        {
            var dateRangeText = GetDateRangeText(model.FromDate, model.ToDate);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("LibraTrack Financial Operations").FontSize(10).SemiBold().FontColor(Color.FromHex(SecondaryColor));
                                titleCol.Item().Text("Fine Revenue & Audit Report").FontSize(18).Bold().FontColor(Color.FromHex(PrimaryColor));
                                titleCol.Item().Text($"Date Range: {dateRangeText}  •  Branch: {model.SelectedBranchName}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(200).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text("FINANCIAL AUDIT").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                metaCol.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8);
                                metaCol.Item().Text("Role: Manager").FontSize(8);
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Color.FromHex(BorderColor));

                        // Summary Metric Cards
                        col.Item().PaddingBottom(12).Row(summaryRow =>
                        {
                            summaryRow.Spacing(10);
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "TOTAL FINES RECORDED", model.TotalFinesCount.ToString(), Colors.Blue.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "TOTAL ASSESSED", $"${model.TotalFinesAssessed:F2}", Colors.Orange.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "COLLECTED / PAID", $"${model.TotalFinesCollected:F2}", Colors.Green.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "OUTSTANDING / UNPAID", $"${model.TotalFinesOutstanding:F2}", Colors.Red.Darken2));
                        });
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(45);  // Loan ID
                            cols.ConstantColumn(75);  // Date
                            cols.RelativeColumn(2);   // Patron
                            cols.ConstantColumn(70);  // Code
                            cols.RelativeColumn(3);   // Title
                            cols.RelativeColumn(2);   // Branch
                            cols.ConstantColumn(65);  // Overdue Days
                            cols.ConstantColumn(65);  // Amount
                            cols.ConstantColumn(75);  // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("# Loan");
                            header.Cell().Element(HeaderCellStyle).Text("Date");
                            header.Cell().Element(HeaderCellStyle).Text("Patron / Card");
                            header.Cell().Element(HeaderCellStyle).Text("Item Code");
                            header.Cell().Element(HeaderCellStyle).Text("Item Title");
                            header.Cell().Element(HeaderCellStyle).Text("Branch");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Overdue Days");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Fine ($)");
                            header.Cell().Element(HeaderCellStyle).Text("Status");
                        });

                        int idx = 0;
                        foreach (var row in model.Rows)
                        {
                            var bg = idx % 2 == 0 ? Colors.White : Color.FromHex(BackgroundLight);
                            idx++;

                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text($"#{row.LoanId}");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.IncidentDate.ToString("yyyy-MM-dd"));
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text($"{row.BorrowerName} ({row.BorrowerCardNumber})");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.LibraryCode).Bold();
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ItemName);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.BranchName);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"{row.DaysOverdue} d");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"${row.FineAmount:F2}").Bold();
                            
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(text =>
                            {
                                if (row.IsPaid)
                                    text.Span("Paid").Bold().FontColor(Colors.Green.Darken2);
                                else
                                    text.Span("Unpaid").Bold().FontColor(Colors.Red.Darken2);
                            });
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("LibraTrack Financial Audit  •  Manager Confidential").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        // =========================================================================
        // 3. Inventory Health PDF
        // =========================================================================
        public static byte[] GenerateInventoryHealthPdf(InventoryHealthReportViewModel model)
        {
            var dateRangeText = GetDateRangeText(model.FromDate, model.ToDate);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("LibraTrack Asset & Fleet Management").FontSize(10).SemiBold().FontColor(Color.FromHex(SecondaryColor));
                                titleCol.Item().Text("Inventory Health & Valuation Audit").FontSize(18).Bold().FontColor(Color.FromHex(PrimaryColor));
                                titleCol.Item().Text($"Filter: {dateRangeText}  •  Branch: {model.SelectedBranchName}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(200).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text("FLEET AUDIT").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                metaCol.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8);
                                metaCol.Item().Text("Role: Manager").FontSize(8);
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Color.FromHex(BorderColor));

                        // Summary Metric Cards
                        col.Item().PaddingBottom(12).Row(summaryRow =>
                        {
                            summaryRow.Spacing(8);
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "TOTAL ITEMS", model.TotalItemsCount.ToString(), Colors.Blue.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "AVAILABLE", model.AvailableCount.ToString(), Colors.Green.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "BORROWED", model.BorrowedCount.ToString(), Colors.Orange.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "DAMAGED/DESTROYED", $"{model.DamagedCount + model.DestroyedCount}", Colors.Red.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "TOTAL ASSET VALUE", $"${model.TotalAssetValue:F2}", Colors.Teal.Darken2));
                            summaryRow.RelativeItem().Element(c => SummaryCard(c, "DAMAGE LIABILITY", $"${model.DamageLiabilityCost:F2}", Colors.Purple.Darken2));
                        });
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40);  // ID
                            cols.ConstantColumn(70);  // Code
                            cols.RelativeColumn(3);   // Title
                            cols.ConstantColumn(60);  // Type
                            cols.RelativeColumn(2);   // Branch
                            cols.ConstantColumn(70);  // Status
                            cols.ConstantColumn(70);  // Cost ($)
                            cols.ConstantColumn(65);  // Loans Count
                            cols.ConstantColumn(75);  // Added Date
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("# ID");
                            header.Cell().Element(HeaderCellStyle).Text("Code");
                            header.Cell().Element(HeaderCellStyle).Text("Item Title");
                            header.Cell().Element(HeaderCellStyle).Text("Category");
                            header.Cell().Element(HeaderCellStyle).Text("Branch Location");
                            header.Cell().Element(HeaderCellStyle).Text("Status");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Replacement ($)");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Circulation");
                            header.Cell().Element(HeaderCellStyle).Text("Added Date");
                        });

                        int idx = 0;
                        foreach (var row in model.Rows)
                        {
                            var bg = idx % 2 == 0 ? Colors.White : Color.FromHex(BackgroundLight);
                            idx++;

                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text($"#{row.ItemId}");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.LibraryCode).Bold();
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ItemName);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.ItemCategory);
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.BranchName);
                            
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(text =>
                            {
                                if (row.Status == Models.ItemStatus.Available)
                                    text.Span(row.Status.ToString()).FontColor(Colors.Green.Darken2);
                                else if (row.Status == Models.ItemStatus.Borrowed)
                                    text.Span(row.Status.ToString()).FontColor(Colors.Orange.Darken2);
                                else
                                    text.Span(row.Status.ToString()).Bold().FontColor(Colors.Red.Darken2);
                            });

                            table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"${row.ReplacementCost:F2}");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"{row.TimesBorrowed}x");
                            table.Cell().Element(c => BodyCellStyle(c, bg)).Text(row.AddedDate.ToString("yyyy-MM-dd"));
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("LibraTrack Inventory Health  •  Manager Confidential").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        // =========================================================================
        // 4. Statistics Summary PDF (for /Manager/Statistics)
        // =========================================================================
        public static byte[] GenerateStatisticsSummaryPdf(ManagerStatisticsViewModel model, string branchName)
        {
            var dateRangeText = GetDateRangeText(model.FromDate, model.ToDate);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("LibraTrack Executive Analytics").FontSize(10).SemiBold().FontColor(Color.FromHex(SecondaryColor));
                                titleCol.Item().Text("Borrowing Statistics & KPI Summary").FontSize(18).Bold().FontColor(Color.FromHex(PrimaryColor));
                                titleCol.Item().Text($"Filter: {dateRangeText}  •  Branch: {branchName}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(180).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text("MANAGEMENT SUMMARY").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                metaCol.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8);
                                metaCol.Item().Text("Role: Manager").FontSize(8);
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Color.FromHex(BorderColor));
                    });

                    page.Content().Column(col =>
                    {
                        // KPI Cards
                        col.Item().PaddingBottom(15).Row(r =>
                        {
                            r.Spacing(10);
                            r.RelativeItem().Element(c => SummaryCard(c, "MOST POPULAR ITEM", model.MostPopularItemName ?? "None", Colors.Blue.Darken2));
                            r.RelativeItem().Element(c => SummaryCard(c, "MOST ACTIVE PATRON", model.MostActiveBorrowerName ?? "None", Colors.Teal.Darken2));
                            r.RelativeItem().Element(c => SummaryCard(c, "AVG LOAN DURATION", $"{model.AverageLoanDurationDays} days", Colors.Purple.Darken2));
                        });

                        col.Item().PaddingTop(10).Text("Overdue Items Requiring Follow-up").FontSize(13).Bold().FontColor(Color.FromHex(PrimaryColor));

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Patron
                                cols.RelativeColumn(4); // Item
                                cols.ConstantColumn(80); // Due Date
                                cols.ConstantColumn(70); // Days Overdue
                                cols.ConstantColumn(80); // Accrued Fine
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCellStyle).Text("Patron");
                                h.Cell().Element(HeaderCellStyle).Text("Item Title");
                                h.Cell().Element(HeaderCellStyle).Text("Due Date");
                                h.Cell().Element(HeaderCellStyle).AlignRight().Text("Days Overdue");
                                h.Cell().Element(HeaderCellStyle).AlignRight().Text("Accruing Fine");
                            });

                            if (!model.OverdueItems.Any())
                            {
                                table.Cell().ColumnSpan(5).Padding(10).AlignCenter().Text("No overdue items currently recorded for this filter.");
                            }
                            else
                            {
                                int idx = 0;
                                foreach (var item in model.OverdueItems)
                                {
                                    var bg = idx % 2 == 0 ? Colors.White : Color.FromHex(BackgroundLight);
                                    idx++;

                                    table.Cell().Element(c => BodyCellStyle(c, bg)).Text(item.BorrowerName);
                                    table.Cell().Element(c => BodyCellStyle(c, bg)).Text(item.ItemName);
                                    table.Cell().Element(c => BodyCellStyle(c, bg)).Text(item.DueDate.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"{item.DaysOverdue} days");
                                    table.Cell().Element(c => BodyCellStyle(c, bg)).AlignRight().Text($"${item.FineAccruing:F2}").Bold().FontColor(Colors.Red.Darken2);
                                }
                            }
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("LibraTrack Executive Reporting  •  Manager Confidential").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        // =========================================================================
        // Helpers & Styling
        // =========================================================================
        private static void SummaryCard(IContainer container, string title, string value, string accentColor)
        {
            container
                .Border(1)
                .BorderColor(Color.FromHex(BorderColor))
                .Background(Color.FromHex(BackgroundLight))
                .Padding(8)
                .Column(c =>
                {
                    c.Item().Text(title).FontSize(7).SemiBold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(2).Text(value).FontSize(13).Bold().FontColor(accentColor);
                });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Color.FromHex(PrimaryColor))
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8));
        }

        private static IContainer BodyCellStyle(IContainer container, string backgroundColor)
        {
            return container
                .Background(backgroundColor)
                .BorderBottom(1)
                .BorderColor(Color.FromHex(BorderColor))
                .PaddingVertical(4)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.FontSize(8));
        }

        private static string GetDateRangeText(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue)
                return $"{from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
            if (from.HasValue)
                return $"From {from.Value:yyyy-MM-dd}";
            if (to.HasValue)
                return $"Up to {to.Value:yyyy-MM-dd}";
            return "All Recorded Dates";
        }
    }
}
