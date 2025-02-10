using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DynastyBeacon.Models;
using System.ComponentModel.DataAnnotations;

namespace DynastyBeacon.Pages.Stocks
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Stock Stock { get; set; } = default!;

        [BindProperty]
        [Display(Name = "Deletion Description")]
        public string DeletionDescription { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.Stocks
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.StockID == id);

            if (stock == null)
            {
                return NotFound();
            }

            Stock = stock;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.Stocks.FindAsync(id);

            if (stock == null)
            {
                return NotFound();
            }

            try
            {
                // Log the deletion for audit purposes
                _logger.LogInformation(
                    "Stock item deletion initiated - ID: {StockId}, Code: {StockCode}, Description: {Description}",
                    stock.StockID,
                    stock.StockCode,
                    DeletionDescription);

                Stock = stock;
                _context.Stocks.Remove(Stock);
                await _context.SaveChangesAsync();

                StatusMessage = "Stock item was successfully deleted.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock item {StockId}", id);
                StatusMessage = "Error: Failed to delete stock item. Please try again.";
                return RedirectToPage("./Delete", new { id });
            }
        }

        private string GenerateAuditReport()
        {
            return $@"
STOCK ITEM DELETION AUDIT REPORT
==============================
Generated on: {DateTime.Now}

BASIC INFORMATION
----------------
Stock Code: {Stock.StockCode}
Category: {Stock.Category}
Description: {Stock.StockDescription}

STOCK STATUS
-----------
Stock On Hand: {Stock.StockOnHand}
Total Purchases: R {Stock.TotalPurchasesExclVat:N2}
Total Sales: R {Stock.TotalSalesExclVat:N2}

DELETION DETAILS
---------------
Deletion Date: {DateTime.Now}
Description: {DeletionDescription}
Deleted By: {User.Identity?.Name ?? "Unknown User"}
";
        }

        public IActionResult OnGetGenerateReport(Guid id)
        {
            if (Stock == null || Stock.StockID != id)
            {
                return NotFound();
            }

            var reportContent = GenerateAuditReport();
            var fileName = $"stock_deletion_report_{Stock.StockCode}_{DateTime.Now:yyyy-MM-dd}.txt";

            return File(System.Text.Encoding.UTF8.GetBytes(reportContent),
                       "text/plain",
                       fileName);
        }
    }
}