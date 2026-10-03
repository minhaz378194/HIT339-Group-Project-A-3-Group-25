using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraTrack.Data;
using LibraTrack.Models;
using LibraTrack.Models.ViewModels;
using LibraTrack.Services;

namespace LibraTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IBookMetadataProvider _metadataProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImportController> _logger;

        private static readonly Regex LibraryCodeRegex = new(@"^[A-Z]{3}-\d{4}$", RegexOptions.Compiled);

        public ImportController(
            LibraryDbContext context,
            IBookMetadataProvider metadataProvider,
            IConfiguration configuration,
            ILogger<ImportController> logger)
        {
            _context = context;
            _metadataProvider = metadataProvider;
            _configuration = configuration;
            _logger = logger;
        }

        // =========================================================================
        // 1. GET: /Import - Landing page showing logs, upload form, mock search
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? query = null, int? lastLogId = null)
        {
            var defaultBranch = _configuration["CurrentBranchCode"] ?? "CEN";

            var model = new ImportIndexViewModel
            {
                DefaultBranchCode = defaultBranch,
                ActiveBranches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(),
                RecentLogs = await _context.ImportLogs.OrderByDescending(l => l.ImportedAt).Take(15).ToListAsync(),
                SearchQuery = query
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchResults = await _metadataProvider.SearchAsync(query);
                model.MockSearchResults = searchResults.ToList();
            }

            if (lastLogId.HasValue)
            {
                var log = await _context.ImportLogs.FindAsync(lastLogId.Value);
                if (log != null)
                {
                    model.LastSummary = new ImportSummaryViewModel
                    {
                        LogId = log.Id,
                        ImportedCount = log.ImportedCount,
                        SkippedCount = log.SkippedCount,
                        ErrorCount = log.ErrorCount,
                        TotalRows = log.TotalRows,
                        ImportedAt = log.ImportedAt
                    };
                }
            }

            return View(model);
        }

        // =========================================================================
        // 2. GET: /Import/DownloadTemplate - Sample CSV for Book, Music, and Toy
        // =========================================================================
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var builder = new StringBuilder();
            // Header line
            builder.AppendLine("ItemType,LibraryCode,Name,Description,BranchCode,ReplacementCost,ImageUrl,Author,Genre,ISBN,PageCount,Publisher,PublicationYear,Artist,ReleaseYear,RecordLabel,Format,DurationMinutes,MusicGenre,ToyType,RecommendedAgeMin,RecommendedAgeMax,Brand,PieceCount,SafetyNotes");
            
            // Sample Book row
            builder.AppendLine("Book,BOK-1008,The Hobbit,A fantasy novel and children's book by English author J. R. R. Tolkien.,CEN,29.95,https://images.unsplash.com/photo-1544947950-fa07a98d237f,J.R.R. Tolkien,Fantasy,978-0547928227,310,Houghton Mifflin,1937,,,,,,,,,,,,");
            
            // Sample Music row
            builder.AppendLine("Music,MUS-2008,Kind of Blue,Legendary studio album by American jazz trumpeter Miles Davis.,CAS,34.50,https://images.unsplash.com/photo-1514525253161-7a46d19cd819,,,,,,,Miles Davis,1959,Columbia Records,Vinyl,45,Modal Jazz,,,,,,");
            
            // Sample Toy row
            builder.AppendLine("Toy,TOY-3008,LEGO Space Rover,Exploration vehicle building set with robotic arm and solar panels.,PAL,49.99,https://images.unsplash.com/photo-1587654780291-39c9404d746b,,,,,,,,,,,,Construction & STEM,6,12,LEGO,275,Small parts - not for children under 3 years.");

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(bytes, "text/csv", "LibraTrack_Catalog_Import_Template.csv");
        }

        // =========================================================================
        // 3. POST: /Import/Upload - Parse CSV and show Preview with Validation
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Error"] = "Please select a valid CSV file to upload.";
                return RedirectToAction(nameof(Index));
            }

            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only comma-separated CSV files (.csv) are supported.";
                return RedirectToAction(nameof(Index));
            }

            var parsedRows = new List<CsvItemRow>();
            var existingCodes = await _context.Items.Select(i => i.LibraryCode.ToUpper()).ToListAsync();
            var existingCodesSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

            var activeBranches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            var branchMap = activeBranches.ToDictionary(b => b.BranchCode.Trim().ToUpper(), b => b, StringComparer.OrdinalIgnoreCase);

            var seenInFileCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var stream = csvFile.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    BadDataFound = null,
                    TrimOptions = TrimOptions.Trim
                };

                using var csv = new CsvReader(reader, config);
                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 1;
                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    var row = new CsvItemRow
                    {
                        RowNumber = rowNumber,
                        ItemType = csv.GetField<string>("ItemType")?.Trim() ?? string.Empty,
                        LibraryCode = csv.GetField<string>("LibraryCode")?.Trim().ToUpperInvariant() ?? string.Empty,
                        Name = csv.GetField<string>("Name")?.Trim() ?? string.Empty,
                        Description = csv.GetField<string>("Description")?.Trim(),
                        BranchCode = csv.GetField<string>("BranchCode")?.Trim().ToUpperInvariant() ?? string.Empty,
                        ImageUrl = csv.GetField<string>("ImageUrl")?.Trim(),

                        // Book
                        Author = csv.GetField<string>("Author")?.Trim(),
                        Genre = csv.GetField<string>("Genre")?.Trim(),
                        ISBN = csv.GetField<string>("ISBN")?.Trim(),
                        Publisher = csv.GetField<string>("Publisher")?.Trim(),

                        // Music
                        Artist = csv.GetField<string>("Artist")?.Trim(),
                        RecordLabel = csv.GetField<string>("RecordLabel")?.Trim(),
                        Format = csv.GetField<string>("Format")?.Trim() ?? "CD",
                        MusicGenre = csv.GetField<string>("MusicGenre")?.Trim(),

                        // Toy
                        ToyType = csv.GetField<string>("ToyType")?.Trim(),
                        Brand = csv.GetField<string>("Brand")?.Trim(),
                        SafetyNotes = csv.GetField<string>("SafetyNotes")?.Trim()
                    };

                    // Decimal parsing for ReplacementCost
                    if (decimal.TryParse(csv.GetField<string>("ReplacementCost"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cost))
                    {
                        row.ReplacementCost = cost;
                    }
                    else
                    {
                        row.ReplacementCost = 25.00m;
                    }

                    // Int parsing
                    if (int.TryParse(csv.GetField<string>("PageCount"), out var pc)) row.PageCount = pc;
                    if (int.TryParse(csv.GetField<string>("PublicationYear"), out var py)) row.PublicationYear = py;
                    if (int.TryParse(csv.GetField<string>("ReleaseYear"), out var ry)) row.ReleaseYear = ry;
                    if (int.TryParse(csv.GetField<string>("DurationMinutes"), out var dm)) row.DurationMinutes = dm;
                    if (int.TryParse(csv.GetField<string>("RecommendedAgeMin"), out var amin)) row.RecommendedAgeMin = amin;
                    if (int.TryParse(csv.GetField<string>("RecommendedAgeMax"), out var amax)) row.RecommendedAgeMax = amax;
                    if (int.TryParse(csv.GetField<string>("PieceCount"), out var pcount)) row.PieceCount = pcount;

                    // VALIDATION LOGIC
                    var errors = new List<string>();

                    // 1. ItemType check
                    var normType = row.ItemType.ToLowerInvariant();
                    if (normType != "book" && normType != "music" && normType != "toy")
                    {
                        errors.Add($"Invalid ItemType '{row.ItemType}' (must be Book, Music, or Toy)");
                    }

                    // 2. Library Code format check
                    if (string.IsNullOrWhiteSpace(row.LibraryCode))
                    {
                        errors.Add("Library code is required");
                    }
                    else if (!LibraryCodeRegex.IsMatch(row.LibraryCode))
                    {
                        errors.Add($"Bad library code format '{row.LibraryCode}' (expected format XXX-0000, e.g., BOK-1010)");
                    }
                    else
                    {
                        // 3. Duplicate checks
                        if (existingCodesSet.Contains(row.LibraryCode))
                        {
                            errors.Add($"Duplicate code '{row.LibraryCode}' (already exists in library catalog)");
                        }
                        else if (seenInFileCodes.Contains(row.LibraryCode))
                        {
                            errors.Add($"Duplicate code '{row.LibraryCode}' (repeated multiple times in this CSV file)");
                        }
                        else
                        {
                            seenInFileCodes.Add(row.LibraryCode);
                        }
                    }

                    // 4. BranchCode check
                    if (string.IsNullOrWhiteSpace(row.BranchCode))
                    {
                        errors.Add("BranchCode is required");
                    }
                    else if (!branchMap.TryGetValue(row.BranchCode, out var branch))
                    {
                        errors.Add($"Unknown or inactive branch code '{row.BranchCode}'");
                    }
                    else
                    {
                        row.ResolvedBranchId = branch.Id;
                        row.ResolvedBranchName = branch.Name;
                    }

                    // 5. Name check
                    if (string.IsNullOrWhiteSpace(row.Name))
                    {
                        errors.Add("Item Name / Title is required");
                    }

                    // 6. Type-specific checks
                    if (normType == "book")
                    {
                        if (string.IsNullOrWhiteSpace(row.Author))
                        {
                            errors.Add("Book author is required");
                        }
                    }
                    else if (normType == "music")
                    {
                        if (string.IsNullOrWhiteSpace(row.Artist))
                        {
                            errors.Add("Music artist or composer is required");
                        }
                    }
                    else if (normType == "toy")
                    {
                        if (string.IsNullOrWhiteSpace(row.ToyType))
                        {
                            errors.Add("Toy category / type is required");
                        }
                    }

                    if (errors.Any())
                    {
                        row.IsValid = false;
                        row.ErrorReason = string.Join("; ", errors);
                    }
                    else
                    {
                        row.IsValid = true;
                    }

                    parsedRows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CSV file {FileName}", csvFile.FileName);
                TempData["Error"] = $"Error reading CSV: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            if (!parsedRows.Any())
            {
                TempData["Error"] = "The uploaded CSV file contains no data rows.";
                return RedirectToAction(nameof(Index));
            }

            // Save rows to Session for subsequent confirmation
            var json = JsonSerializer.Serialize(parsedRows);
            HttpContext.Session.SetString("ImportPreviewRows", json);
            HttpContext.Session.SetString("ImportPreviewFileName", csvFile.FileName);

            var previewModel = new ImportPreviewViewModel
            {
                FileName = csvFile.FileName,
                TotalRows = parsedRows.Count,
                ValidCount = parsedRows.Count(r => r.IsValid),
                InvalidCount = parsedRows.Count(r => !r.IsValid),
                Rows = parsedRows
            };

            return View("Preview", previewModel);
        }

        // =========================================================================
        // 4. POST: /Import/Confirm - Commit valid rows inside database transaction
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm()
        {
            var json = HttpContext.Session.GetString("ImportPreviewRows");
            var fileName = HttpContext.Session.GetString("ImportPreviewFileName") ?? "catalog_import.csv";

            if (string.IsNullOrEmpty(json))
            {
                TempData["Error"] = "No pending import session found or session expired. Please re-upload your CSV file.";
                return RedirectToAction(nameof(Index));
            }

            var rows = JsonSerializer.Deserialize<List<CsvItemRow>>(json) ?? new List<CsvItemRow>();
            var validRows = rows.Where(r => r.IsValid).ToList();
            var invalidRows = rows.Where(r => !r.IsValid).ToList();

            if (!validRows.Any())
            {
                TempData["Error"] = "There are no valid records in the uploaded file to import.";
                return RedirectToAction(nameof(Index));
            }

            var username = User.Identity?.Name ?? "admin";
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                userId = parsedId;
            }

            int importedCount = 0;
            var executionErrors = new List<CsvItemRow>();

            // Execute import inside an atomic database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Re-verify codes against DB to prevent race condition duplicates
                var existingDbCodes = await _context.Items
                    .Where(i => validRows.Select(vr => vr.LibraryCode).Contains(i.LibraryCode))
                    .Select(i => i.LibraryCode)
                    .ToListAsync();
                var conflictSet = new HashSet<string>(existingDbCodes, StringComparer.OrdinalIgnoreCase);

                foreach (var row in validRows)
                {
                    if (conflictSet.Contains(row.LibraryCode))
                    {
                        row.IsValid = false;
                        row.ErrorReason = $"Library code '{row.LibraryCode}' was registered concurrently by another user.";
                        invalidRows.Add(row);
                        continue;
                    }

                    var itemType = row.ItemType.ToLowerInvariant();
                    Item newItem;

                    if (itemType == "book")
                    {
                        newItem = new Book
                        {
                            LibraryCode = row.LibraryCode,
                            Name = row.Name,
                            Description = row.Description ?? string.Empty,
                            BranchId = row.ResolvedBranchId!.Value,
                            ReplacementCost = row.ReplacementCost ?? 25.00m,
                            ImageUrl = row.ImageUrl,
                            Status = ItemStatus.Available,
                            CreatedAt = DateTime.UtcNow,
                            Author = row.Author ?? "Unknown Author",
                            Genre = !string.IsNullOrWhiteSpace(row.Genre) ? row.Genre : "General",
                            ISBN = row.ISBN,
                            PageCount = row.PageCount,
                            Publisher = row.Publisher,
                            PublicationYear = row.PublicationYear
                        };
                    }
                    else if (itemType == "music")
                    {
                        newItem = new Music
                        {
                            LibraryCode = row.LibraryCode,
                            Name = row.Name,
                            Description = row.Description ?? string.Empty,
                            BranchId = row.ResolvedBranchId!.Value,
                            ReplacementCost = row.ReplacementCost ?? 25.00m,
                            ImageUrl = row.ImageUrl,
                            Status = ItemStatus.Available,
                            CreatedAt = DateTime.UtcNow,
                            Artist = row.Artist ?? "Unknown Artist",
                            ReleaseYear = row.ReleaseYear ?? DateTime.UtcNow.Year,
                            RecordLabel = row.RecordLabel,
                            Format = !string.IsNullOrWhiteSpace(row.Format) ? row.Format : "CD",
                            DurationMinutes = row.DurationMinutes,
                            Genre = row.MusicGenre
                        };
                    }
                    else // toy
                    {
                        newItem = new Toy
                        {
                            LibraryCode = row.LibraryCode,
                            Name = row.Name,
                            Description = row.Description ?? string.Empty,
                            BranchId = row.ResolvedBranchId!.Value,
                            ReplacementCost = row.ReplacementCost ?? 25.00m,
                            ImageUrl = row.ImageUrl,
                            Status = ItemStatus.Available,
                            CreatedAt = DateTime.UtcNow,
                            ToyType = row.ToyType ?? "General Toy",
                            RecommendedAgeMin = row.RecommendedAgeMin ?? 3,
                            RecommendedAgeMax = row.RecommendedAgeMax,
                            Brand = row.Brand,
                            PieceCount = row.PieceCount,
                            SafetyNotes = row.SafetyNotes
                        };
                    }

                    _context.Items.Add(newItem);
                    importedCount++;
                }

                await _context.SaveChangesAsync();

                // Save Audit Log
                var importLog = new ImportLog
                {
                    UserId = userId,
                    Username = username,
                    ImportedAt = DateTime.UtcNow,
                    Source = $"CSV Upload ({fileName})",
                    TotalRows = rows.Count,
                    ImportedCount = importedCount,
                    SkippedCount = invalidRows.Count,
                    ErrorCount = invalidRows.Count,
                    Notes = $"Successfully imported {importedCount} catalog items. {invalidRows.Count} invalid rows were skipped.",
                    ErrorReportJson = invalidRows.Any() ? JsonSerializer.Serialize(invalidRows) : null
                };

                _context.ImportLogs.Add(importLog);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Clear session cache
                HttpContext.Session.Remove("ImportPreviewRows");
                HttpContext.Session.Remove("ImportPreviewFileName");

                TempData["Success"] = $"Import successful! Imported {importedCount} new item(s) to the catalog.";
                if (invalidRows.Any())
                {
                    TempData["Warning"] = $"{invalidRows.Count} row(s) had validation errors and were skipped. You can download the error report below.";
                }

                return RedirectToAction(nameof(Index), new { lastLogId = importLog.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed while importing CSV data.");
                TempData["Error"] = $"Import failed due to a database error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================================
        // 5. GET: /Import/DownloadErrorReport - Download CSV of invalid rows with reasons
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> DownloadErrorReport(int logId)
        {
            var log = await _context.ImportLogs.FindAsync(logId);
            if (log == null || string.IsNullOrEmpty(log.ErrorReportJson))
            {
                TempData["Error"] = "Error report not found or this import had zero errors.";
                return RedirectToAction(nameof(Index));
            }

            var invalidRows = JsonSerializer.Deserialize<List<CsvItemRow>>(log.ErrorReportJson) ?? new List<CsvItemRow>();

            var builder = new StringBuilder();
            builder.AppendLine("RowNumber,ItemType,LibraryCode,Name,BranchCode,ErrorReason");

            foreach (var r in invalidRows)
            {
                var cleanReason = (r.ErrorReason ?? string.Empty).Replace("\"", "\"\"");
                var cleanName = (r.Name ?? string.Empty).Replace("\"", "\"\"");
                builder.AppendLine($"{r.RowNumber},{r.ItemType},{r.LibraryCode},\"{cleanName}\",{r.BranchCode},\"{cleanReason}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(bytes, "text/csv", $"Import_Errors_Log_{log.Id}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        // =========================================================================
        // 6. POST: /Import/ImportMockBook - Add book directly from Mock Metadata search
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportMockBook(AddMockBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid book details supplied.";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }

            var cleanCode = model.LibraryCode.Trim().ToUpperInvariant();

            // Validate Library Code format
            if (!LibraryCodeRegex.IsMatch(cleanCode))
            {
                TempData["Error"] = $"Invalid library code format '{cleanCode}'. Must match 'XXX-0000' (e.g. BOK-1010).";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }

            // Check duplicate code
            if (await _context.Items.AnyAsync(i => i.LibraryCode == cleanCode))
            {
                TempData["Error"] = $"Library code '{cleanCode}' already exists in catalog. Please choose a different code.";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }

            // Verify Branch
            var branch = await _context.Branches.FindAsync(model.BranchId);
            if (branch == null || !branch.IsActive)
            {
                TempData["Error"] = "Selected branch does not exist or is inactive.";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }

            var username = User.Identity?.Name ?? "admin";
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                userId = parsedId;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newBook = new Book
                {
                    LibraryCode = cleanCode,
                    Name = model.Title.Trim(),
                    Author = model.Author.Trim(),
                    Genre = !string.IsNullOrWhiteSpace(model.Genre) ? model.Genre.Trim() : "General",
                    ISBN = model.ISBN?.Trim(),
                    PageCount = model.PageCount,
                    Publisher = model.Publisher?.Trim(),
                    PublicationYear = model.PublicationYear,
                    Description = model.Description?.Trim() ?? string.Empty,
                    ImageUrl = model.ImageUrl?.Trim(),
                    ReplacementCost = model.ReplacementCost > 0 ? model.ReplacementCost : 25.00m,
                    BranchId = branch.Id,
                    Status = ItemStatus.Available,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Books.Add(newBook);
                await _context.SaveChangesAsync();

                var log = new ImportLog
                {
                    UserId = userId,
                    Username = username,
                    ImportedAt = DateTime.UtcNow,
                    Source = "Mock Metadata Search",
                    TotalRows = 1,
                    ImportedCount = 1,
                    SkippedCount = 0,
                    ErrorCount = 0,
                    Notes = $"Imported '{newBook.Name}' ({newBook.LibraryCode}) [ISBN: {newBook.ISBN ?? "N/A"}] to {branch.Name} via Mock Metadata Provider."
                };

                _context.ImportLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = $"Successfully added '{newBook.Name}' ({newBook.LibraryCode}) to {branch.Name}!";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to import book from metadata provider");
                TempData["Error"] = $"Error saving item: {ex.Message}";
                return RedirectToAction(nameof(Index), new { query = model.Title });
            }
        }

        // =========================================================================
        // Helper: Generate candidate LibraryCode
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> GetNextLibraryCode(string prefix = "BOK")
        {
            var cleanPrefix = prefix.ToUpperInvariant();
            var existingCodes = await _context.Items
                .Where(i => i.LibraryCode.StartsWith(cleanPrefix + "-"))
                .Select(i => i.LibraryCode)
                .ToListAsync();

            int maxNum = 1000;
            foreach (var code in existingCodes)
            {
                var parts = code.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out var num))
                {
                    if (num > maxNum) maxNum = num;
                }
            }

            var nextCode = $"{cleanPrefix}-{maxNum + 1:D4}";
            return Json(new { nextCode });
        }
    }
}
