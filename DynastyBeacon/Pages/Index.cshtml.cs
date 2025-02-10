using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DynastyBeacon.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private const decimal VAT_RATE = 0.15m; // South African VAT rate (15%)

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;

            // Set South African culture for currency formatting
            var zaCulture = new CultureInfo("en-ZA");
            CultureInfo.DefaultThreadCurrentCulture = zaCulture;
            CultureInfo.DefaultThreadCurrentUICulture = zaCulture;
        }

        public class DashboardStats
        {
            // Header Card Statistics
            public decimal TotalRevenue { get; set; }
            public int TotalDebtors { get; set; }
            public int TotalStockItems { get; set; }
            public int PendingInvoices { get; set; }

            // Additional Financial Statistics
            public decimal TotalVAT => TotalRevenue * 0.15m;
            public decimal TotalRevenueIncVAT => TotalRevenue * 1.15m;
            public decimal AverageInvoiceValue { get; set; }
            public decimal MonthlyGrowthRate { get; set; }

            // Collections
            public List<CategoryStats> StockByCategory { get; set; } = new();
            public List<RecentActivity> RecentActivities { get; set; } = new();
        }

        public class CategoryStats
        {
            public string Category { get; set; } = string.Empty;
            public int StockCount { get; set; }
            public decimal StockValue { get; set; }
            public decimal StockValueIncVAT => StockValue * 1.15m;
        }

        public class RecentActivity
        {
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public string Icon => Type switch
            {
                "Invoice" => "fa-file-invoice",
                "Stock Update" => "fa-box",
                "Debtor Update" => "fa-user-edit",
                _ => "fa-info-circle"
            };
            public string Color => Type switch
            {
                "Invoice" => "#0A2342", // primary-navy
                "Stock Update" => "#C5A368", // accent-gold
                "Debtor Update" => "#2D2D2D", // text-dark
                _ => "#6c757d"
            };
        }

        public DashboardStats Stats { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Calculate date ranges
                var today = DateTime.UtcNow.Date;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);

                // Basic counts
                Stats.TotalDebtors = await _context.Debtors.CountAsync();
                Stats.TotalStockItems = await _context.Stocks.CountAsync();
                Stats.PendingInvoices = await _context.InvoiceHeaders
                    .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate <= endOfMonth)
                    .CountAsync();

                // Revenue calculations
                var currentMonthRevenue = await _context.InvoiceHeaders
                    .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate <= endOfMonth)
                    .SumAsync(i => i.TotalSellAmountExclVAT);

                var lastMonthRevenue = await _context.InvoiceHeaders
                    .Where(i => i.InvoiceDate >= startOfLastMonth && i.InvoiceDate < startOfMonth)
                    .SumAsync(i => i.TotalSellAmountExclVAT);

                Stats.TotalRevenue = currentMonthRevenue;

                // Calculate monthly growth rate
                if (lastMonthRevenue > 0)
                {
                    Stats.MonthlyGrowthRate = ((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
                }

                // Calculate average invoice value
                var invoiceCount = await _context.InvoiceHeaders
                    .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate <= endOfMonth)
                    .CountAsync();

                if (invoiceCount > 0)
                {
                    Stats.AverageInvoiceValue = currentMonthRevenue / invoiceCount;
                }

                // Get stock by category
                Stats.StockByCategory = await _context.Stocks
                    .GroupBy(s => s.Category)
                    .Select(g => new CategoryStats
                    {
                        Category = g.Key,
                        StockCount = g.Count(),
                        StockValue = g.Sum(s => s.Cost * s.StockOnHand)
                    })
                    .OrderByDescending(c => c.StockCount)
                    .ToListAsync();

                // Get recent activities
                Stats.RecentActivities = await GetRecentActivities();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard statistics");
                TempData["ErrorMessage"] = "Error loading dashboard data. Please try refreshing the page.";
                return Page();
            }
        }

        private async Task<List<RecentActivity>> GetRecentActivities()
        {
            var invoiceActivities = await _context.InvoiceHeaders
                .OrderByDescending(i => i.InvoiceDate)
                .Take(5)
                .Select(i => new RecentActivity
                {
                    Type = "Invoice",
                    Description = $"Invoice #{i.InvoiceNo} - R{i.TotalSellAmountExclVAT:N2} (excl. VAT)",
                    Date = i.InvoiceDate
                })
                .ToListAsync();

            var stockActivities = await _context.Stocks
                .Where(s => s.UpdatedOn != null)
                .OrderByDescending(s => s.UpdatedOn)
                .Take(5)
                .Select(s => new RecentActivity
                {
                    Type = "Stock Update",
                    Description = $"Updated {s.StockDescription}",
                    Date = s.UpdatedOn.Value
                })
                .ToListAsync();

            var debtorActivities = await _context.Debtors
                .Where(d => d.UpdatedOn != null)
                .OrderByDescending(d => d.UpdatedOn)
                .Take(5)
                .Select(d => new RecentActivity
                {
                    Type = "Debtor Update",
                    Description = $"Updated {d.Name}",
                    Date = d.UpdatedOn.Value
                })
                .ToListAsync();

            return invoiceActivities
                .Concat(stockActivities)
                .Concat(debtorActivities)
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToList();
        }

        public string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2", new CultureInfo("en-ZA"));
        }
    }
}