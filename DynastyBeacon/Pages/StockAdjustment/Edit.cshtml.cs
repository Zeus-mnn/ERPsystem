using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DynastyBeacon.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using System.Text;

namespace DynastyBeacon.Pages.StockAdjustment
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Read-only display properties
        public string StockCode { get; set; }
        public string StockDescription { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; }

        [BindProperty]
        public Guid StockID { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Adjustment amount is required")]
        [Display(Name = "Adjustment Amount")]
        public int AdjustmentAmount { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please provide a reason for the adjustment")]
        [Display(Name = "Reason for Adjustment")]
        public string AdjustmentReason { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid stockId)
        {
            var stock = await _context.Stocks
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.StockID == stockId);

            if (stock == null)
            {
                return NotFound();
            }

            // Set display properties
            StockID = stock.StockID;
            StockCode = stock.StockCode;
            StockDescription = stock.StockDescription;
            Category = stock.Category;
            CurrentStock = stock.StockOnHand;

            return Page();
        }

        private string GenerateStockAdjustmentReport(Stock stock, int originalStock, int newStock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("STOCK ADJUSTMENT REPORT");
            sb.AppendLine("======================");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("");
            sb.AppendLine("STOCK INFORMATION");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Stock Code: {stock.StockCode}");
            sb.AppendLine($"Description: {stock.StockDescription}");
            sb.AppendLine($"Category: {stock.Category}");
            sb.AppendLine("");
            sb.AppendLine("ADJUSTMENT DETAILS");
            sb.AppendLine("------------------");
            sb.AppendLine($"Original Stock: {originalStock}");
            sb.AppendLine($"Adjustment Amount: {AdjustmentAmount}");
            sb.AppendLine($"New Stock Level: {newStock}");
            sb.AppendLine("");
            sb.AppendLine("REASON FOR ADJUSTMENT");
            sb.AppendLine("--------------------");
            sb.AppendLine(AdjustmentReason);
            sb.AppendLine("");
            sb.AppendLine("AUDIT INFORMATION");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Adjusted By: {User.Identity?.Name ?? "System"}");
            sb.AppendLine($"Adjustment Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return sb.ToString();
        }

        public IActionResult OnGetDownloadReport(Guid id)
        {
            var stock = _context.Stocks.FirstOrDefault(s => s.StockID == id);
            if (stock == null)
            {
                return NotFound();
            }

            var reportContent = GenerateStockAdjustmentReport(stock, stock.StockOnHand, stock.StockOnHand);
            var bytes = Encoding.UTF8.GetBytes(reportContent);
            return File(bytes, "text/plain", $"StockAdjustment_{stock.StockCode}_{DateTime.Now:yyyyMMddHHmmss}.txt");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                return Page();
            }

            var stock = await _context.Stocks.FindAsync(StockID);
            if (stock == null)
            {
                _logger.LogWarning("Stock item not found for ID: {StockID}", StockID);
                ModelState.AddModelError("", "Stock item not found.");
                return Page();
            }

            // Calculate new stock level
            int originalStock = stock.StockOnHand;
            int newStockLevel = stock.StockOnHand + AdjustmentAmount;

            if (newStockLevel < 0)
            {
                ModelState.AddModelError("AdjustmentAmount", "Adjustment would result in negative stock.");
                PopulateDisplayProperties(stock);
                return Page();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create new stock transaction
                    var stockTransaction = new StockTransaction
                    {
                        TransactionID = Guid.NewGuid(),
                        StockID = stock.StockID,
                        TransactionDate = DateTime.UtcNow,
                        TransactionType = AdjustmentAmount > 0 ? "Stock Adjustment (Add)" : "Stock Adjustment (Remove)",
                        DocumentNo = $"ADJ{DateTime.Now:yyyyMMddHHmmss}",
                        Qty = AdjustmentAmount,
                        UnitCost = stock.Cost,
                        UnitSell = stock.SellingPrice,
                        CreatedOn = DateTime.UtcNow
                    };

                    // Update stock quantity
                    stock.StockOnHand = newStockLevel;
                    stock.UpdatedBy = User.Identity?.Name ?? "System";
                    stock.UpdatedOn = DateTime.UtcNow;

                    _context.StockTransactions.Add(stockTransaction);

                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Stock adjustment completed - ID: {StockId}, Code: {StockCode}, From: {Original} To: {New}, Document: {Document}",
                            stock.StockID,
                            stock.StockCode,
                            originalStock,
                            newStockLevel,
                            stockTransaction.DocumentNo);

                        TempData["SuccessMessage"] = $"Stock adjustment for {stock.StockCode} completed successfully.";

                        // Update display properties with new values
                        PopulateDisplayProperties(stock);
                        CurrentStock = newStockLevel;

                        return Page();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        await transaction.RollbackAsync();

                        _logger.LogError(dbEx,
                            "Database update error during stock adjustment - StockID: {StockId}, AdjustmentAmount: {AdjustmentAmount}, Error: {ErrorMessage}",
                            StockID,
                            AdjustmentAmount,
                            dbEx.Message);

                        // Log inner exceptions
                        var innerEx = dbEx.InnerException;
                        while (innerEx != null)
                        {
                            _logger.LogError(innerEx, "Inner Exception: {ErrorMessage}", innerEx.Message);
                            innerEx = innerEx.InnerException;
                        }

                        // Handle SQL-specific errors
                        if (dbEx.InnerException is SqlException sqlEx)
                        {
                            _logger.LogError("SQL Error Number: {ErrorNumber}, Message: {ErrorMessage}",
                                sqlEx.Number,
                                sqlEx.Message);

                            switch (sqlEx.Number)
                            {
                                case 271:  // Computed column error
                                    ModelState.AddModelError("", "An error occurred with transaction code generation. Please contact the system administrator.");
                                    break;
                                case 544:  // Identity column error
                                    ModelState.AddModelError("", "A database configuration error occurred. Please contact system administrator.");
                                    break;
                                case 2627: // Unique constraint error
                                    ModelState.AddModelError("", "A duplicate transaction was detected. Please try again.");
                                    break;
                                default:
                                    ModelState.AddModelError("", $"A database error occurred (Code: {sqlEx.Number}). Please contact the system administrator.");
                                    break;
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "An unexpected database error occurred while adjusting stock.");
                        }

                        PopulateDisplayProperties(stock);
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex,
                        "Unexpected error during stock adjustment - StockID: {StockId}, AdjustmentAmount: {AdjustmentAmount}",
                        StockID,
                        AdjustmentAmount);

                    ModelState.AddModelError("", "An unexpected error occurred while adjusting stock.");

                    PopulateDisplayProperties(stock);
                    return Page();
                }
            }
        }

        // Helper method to reduce code duplication
        private void PopulateDisplayProperties(Stock stock)
        {
            StockCode = stock.StockCode;
            StockDescription = stock.StockDescription;
            Category = stock.Category;
            CurrentStock = stock.StockOnHand;
        }
    }
}