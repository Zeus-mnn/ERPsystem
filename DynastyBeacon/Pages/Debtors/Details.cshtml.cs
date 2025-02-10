using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DynastyBeacon.Models;

namespace DynastyBeacon.Pages.Debtors
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

        public Debtor Debtor { get; set; } = default!;

        // Calculated properties
        public decimal ProfitMargin => Debtor.SalesYearToDate > 0
            ? ((Debtor.SalesYearToDate - Debtor.CostYearToDate) / Debtor.SalesYearToDate) * 100
            : 0;

        public decimal GrossProfit => Debtor.SalesYearToDate - Debtor.CostYearToDate;

        public string DebtorStatus => Debtor.Balance switch
        {
            0 => "Paid",                         
            > 0 and <= 10000 => "Good Standing", 
            > 10000 => "Review Required",       
            _ => "Unknown"                       
        };

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var debtor = await _context.Debtors
                    .FirstOrDefaultAsync(m => m.DebtorID == id);

                if (debtor == null)
                {
                    return NotFound();
                }

                Debtor = debtor;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving debtor details for ID: {DebtorId}", id);
                TempData["ErrorMessage"] = "Error loading debtor details. Please try again.";
                return RedirectToPage("./Index");
            }
        }

        public string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2", new CultureInfo("en-ZA"));
        }

        public string GetBalanceStatusClass()
        {
            return DebtorStatus switch
            {
                "Paid" => "text-success",
                "Good Standing" => "text-primary",
                "Review Required" => "text-warning",
                _ => "text-muted"
            };
        }
    }
}