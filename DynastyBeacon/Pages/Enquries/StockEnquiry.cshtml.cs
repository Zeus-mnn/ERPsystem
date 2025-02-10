using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DynastyBeacon.Models;
using System.Text.Json;

namespace DynastyBeacon.Pages.Enquiries
{
    public class StockEnquiryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StockEnquiryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Stock> Stocks { get; set; } = new List<Stock>();
        public IList<string> Categories { get; set; } = new List<string>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        public async Task OnGetAsync()
        {
            // Get distinct categories for the filter dropdown
            Categories = await _context.Stocks
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Start with all stocks
            var query = _context.Stocks.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchTermLower = SearchTerm.ToLower();
                query = query.Where(s =>
                    s.StockCode.ToLower().Contains(searchTermLower) ||
                    s.StockDescription.ToLower().Contains(searchTermLower) ||
                    s.Category.ToLower().Contains(searchTermLower)
                );
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(Category))
            {
                query = query.Where(s => s.Category == Category);
            }

            // Apply sorting
            query = SortOrder?.ToLower() switch
            {
                "description" => query.OrderBy(s => s.StockDescription),
                "description_desc" => query.OrderByDescending(s => s.StockDescription),
                "stock_level" => query.OrderBy(s => s.StockOnHand),
                "stock_level_desc" => query.OrderByDescending(s => s.StockOnHand),
                "price" => query.OrderBy(s => s.SellingPrice),
                "price_desc" => query.OrderByDescending(s => s.SellingPrice),
                _ => query.OrderBy(s => s.StockCode)
            };

            Stocks = await query.ToListAsync();
        }

        public async Task<IActionResult> OnGetStockDetailsAsync(Guid stockId)
        {
            var stock = await _context.Stocks
                .Include(s => s.StockTransactions)
                .FirstOrDefaultAsync(s => s.StockID == stockId);

            if (stock == null)
            {
                return NotFound();
            }

            // Get transactions for the last 12 months
            var lastYear = DateTime.Now.AddYears(-1);
            var transactions = stock.StockTransactions
                .Where(t => t.TransactionDate >= lastYear)
                .OrderByDescending(t => t.TransactionDate)
                .Take(50)
                .ToList();

            // Calculate transaction statistics
            var transactionsByType = transactions
                .GroupBy(t => t.TransactionType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate monthly movement
            var monthlyMovement = transactions
                .GroupBy(t => new {
                    Month = t.TransactionDate.ToString("MMM yyyy"),
                    Date = new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1)
                })
                .OrderBy(g => g.Key.Date)
                .Select(g => new
                {
                    month = g.Key.Month,
                    purchases = g.Where(t => t.TransactionType.Contains("Purchase")).Sum(t => t.Qty),
                    sales = g.Where(t => t.TransactionType.Contains("Sale")).Sum(t => t.Qty)
                })
                .ToList();

            var stockDetails = new
            {
                basicInfo = new
                {
                    stock.StockID,
                    stock.StockCode,
                    stock.StockDescription,
                    stock.Category,
                    stock.Cost,
                    stock.SellingPrice,
                    stock.StockOnHand,
                    stock.QtyPurchased,
                    stock.QtySold,
                    stock.TotalPurchasesExclVat,
                    stock.TotalSalesExclVat,
                    stock.CreatedBy,
                    stock.CreatedOn,
                    stock.UpdatedBy,
                    stock.UpdatedOn
                },
                transactions = transactions.Select(t => new
                {
                    t.TransactionDate,
                    t.TransactionType,
                    t.DocumentNo,
                    t.Qty,
                    t.UnitCost,
                    t.UnitSell
                }),
                transactionsByType,
                monthlyMovement,
                totalTransactions = transactions.Count,
                averageQuantity = transactions.Any() ? Math.Round(transactions.Average(t => t.Qty), 2) : 0,
                largestTransaction = transactions.OrderByDescending(t => t.Qty).FirstOrDefault(),
                mostRecentTransaction = transactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault()
            };

            return new JsonResult(stockDetails);
        }
    }
}