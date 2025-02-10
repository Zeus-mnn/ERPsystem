using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DynastyBeacon.Models;  // Add this to reference the Stock model

namespace DynastyBeacon.Pages.Stocks
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private const decimal VAT_RATE = 0.15m; // South African VAT rate (15%)

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;

            // Set South African culture for currency formatting
            var zaCulture = new CultureInfo("en-ZA");
            CultureInfo.DefaultThreadCurrentCulture = zaCulture;
            CultureInfo.DefaultThreadCurrentUICulture = zaCulture;
        }

        public Stock Stock { get; set; } = default!;  // Changed from Stocks to Stock

        public decimal GrossProfit => Stock.TotalSalesExclVat - Stock.TotalPurchasesExclVat;

        public decimal ProfitMargin => Stock.TotalSalesExclVat > 0
            ? (GrossProfit / Stock.TotalSalesExclVat) * 100
            : 0;

        public decimal TotalValueIncVAT => Stock.Cost * Stock.StockOnHand * (1 + VAT_RATE);

        public string StockStatus => Stock.StockOnHand switch
        {
            <= 0 => "Out of Stock",
            <= 5 => "Low Stock",
            _ => "In Stock"
        };

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(m => m.StockID == id);

                if (stock == null)
                {
                    return NotFound();
                }

                Stock = stock;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock details for ID: {StockId}", id);
                TempData["ErrorMessage"] = "Error loading stock details. Please try again.";
                return RedirectToPage("./Index");
            }
        }

        public string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2", new CultureInfo("en-ZA"));
        }

        public string GetStockStatusClass()
        {
            return StockStatus switch
            {
                "Out of Stock" => "text-danger",
                "Low Stock" => "text-warning",
                _ => "text-success"
            };
        }
    }
}