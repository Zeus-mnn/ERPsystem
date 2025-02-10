using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DynastyBeacon.Models;
using System.ComponentModel.DataAnnotations;

namespace DynastyBeacon.Pages.Invoices
{
    public class CreateInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateInvoiceModel> _logger;
        private const decimal VAT_RATE = 0.15m; // 15% VAT rate

        public CreateInvoiceModel(ApplicationDbContext context, ILogger<CreateInvoiceModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InvoiceInputModel Input { get; set; }

        public class InvoiceInputModel
        {
            [Required]
            public Guid DebtorID { get; set; }

            [Required]
            public DateTime InvoiceDate { get; set; } = DateTime.Now;

            [Required]
            public List<InvoiceLineItem> LineItems { get; set; } = new();

            public decimal SubTotal => LineItems?.Sum(x => x.Total) ?? 0;
            public decimal VatAmount => SubTotal * VAT_RATE;
            public decimal GrandTotal => SubTotal + VatAmount;
        }

        public class InvoiceLineItem
        {
            public Guid StockID { get; set; }
            public string StockCode { get; set; }
            public string Description { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
            public decimal Total => (Quantity * UnitPrice) - Discount;
            public decimal UnitCost { get; set; } // Hidden from view, used for calculations
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public Debtor SelectedDebtor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<JsonResult> OnGetSearchDebtorsAsync(string searchTerm)
        {
            var debtors = await _context.Debtors
                .Where(d => d.Name.Contains(searchTerm) ||
                           d.AccountCode.Contains(searchTerm) ||
                           d.Email.Contains(searchTerm))
                .Select(d => new
                {
                    d.DebtorID,
                    d.AccountCode,
                    d.Name,
                    d.Balance,
                    d.Email
                })
                .Take(10)
                .ToListAsync();

            return new JsonResult(debtors);
        }

        public async Task<JsonResult> OnGetSearchStockAsync(string searchTerm)
        {
            var stocks = await _context.Stocks
                .Where(s => s.StockCode.Contains(searchTerm) ||
                           s.StockDescription.Contains(searchTerm))
                .Select(s => new
                {
                    s.StockID,
                    s.StockCode,
                    s.StockDescription,
                    s.SellingPrice,
                    s.Cost,
                    s.StockOnHand
                })
                .Take(10)
                .ToListAsync();

            return new JsonResult(stocks);
        }

        public async Task<JsonResult> OnGetDebtorDetailsAsync(Guid debtorId)
        {
            var debtor = await _context.Debtors
                .Where(d => d.DebtorID == debtorId)
                .Select(d => new
                {
                    d.DebtorID,
                    d.AccountCode,
                    d.Name,
                    d.Balance,
                    d.Email,
                    d.Address,
                    d.Phone
                })
                .FirstOrDefaultAsync();

            return new JsonResult(debtor);
        }

        public async Task<JsonResult> OnGetStockDetailsAsync(Guid stockId)
        {
            var stock = await _context.Stocks
                .Where(s => s.StockID == stockId)
                .Select(s => new
                {
                    s.StockID,
                    s.StockCode,
                    s.StockDescription,
                    s.SellingPrice,
                    s.Cost,
                    s.StockOnHand
                })
                .FirstOrDefaultAsync();

            return new JsonResult(stock);
        }

        public async Task<IActionResult> OnPostProcessInvoiceAsync(InvoiceInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Invoice Header
                var invoiceHeader = new InvoiceHeader
                {
                    InvoiceID = Guid.NewGuid(),
                    DebtorID = input.DebtorID,
                    InvoiceDate = input.InvoiceDate,
                    TotalSellAmountExclVAT = input.SubTotal,
                    VAT = input.VatAmount,
                    TotalCost = input.LineItems.Sum(x => x.UnitCost * x.Quantity),
                    CreatedOn = DateTime.Now
                };

                _context.InvoiceHeaders.Add(invoiceHeader);

                // 2. Create Invoice Details
                foreach (var item in input.LineItems)
                {
                    var detail = new InvoiceDetail
                    {
                        InvoiceDetailID = Guid.NewGuid(),
                        InvoiceID = invoiceHeader.InvoiceID,
                        StockID = item.StockID,
                        QtySold = item.Quantity,
                        UnitCost = item.UnitCost,
                        UnitSell = item.UnitPrice,
                        Disc = item.Discount,
                        Total = item.Total,
                        CreatedOn = DateTime.Now
                    };

                    _context.InvoiceDetails.Add(detail);

                    // 3. Update Stock
                    var stock = await _context.Stocks.FindAsync(item.StockID);
                    if (stock == null) throw new Exception($"Stock {item.StockCode} not found");

                    stock.StockOnHand -= item.Quantity;
                    stock.QtySold += item.Quantity;
                    stock.TotalSalesExclVat += item.Total;
                    stock.UpdatedBy = "System";
                    stock.UpdatedOn = DateTime.Now;

                    // 4. Create Stock Transaction
                    var stockTransaction = new StockTransaction
                    {
                        TransactionID = Guid.NewGuid(),
                        StockID = item.StockID,
                        TransactionDate = DateTime.Now,
                        TransactionType = "SALE",
                        DocumentNo = invoiceHeader.InvoiceNo,
                        Qty = item.Quantity,
                        UnitCost = item.UnitCost,
                        UnitSell = item.UnitPrice,
                        CreatedOn = DateTime.Now
                    };

                    _context.StockTransactions.Add(stockTransaction);
                }

                // 5. Update Debtor
                var debtor = await _context.Debtors.FindAsync(input.DebtorID);
                if (debtor == null) throw new Exception("Debtor not found");

                debtor.Balance += input.GrandTotal;
                debtor.SalesYearToDate += input.SubTotal;
                debtor.CostYearToDate += input.LineItems.Sum(x => x.UnitCost * x.Quantity);
                debtor.UpdatedBy = "System";
                debtor.UpdatedOn = DateTime.Now;

                // 6. Create Debtor Transaction
                var debtorTransaction = new DebtorTransaction
                {
                    TransactionID = Guid.NewGuid(),
                    DebtorID = input.DebtorID,
                    TransactionDate = DateTime.Now,
                    TransactionType = "SALE",
                    DocumentNo = invoiceHeader.InvoiceNo,
                    GrossTransactionValue = input.GrandTotal,
                    VatValue = input.VatAmount,
                    CreatedOn = DateTime.Now
                };

                _context.DebtorTransactions.Add(debtorTransaction);

                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Generate and return invoice PDF
                await GenerateInvoicePdf(invoiceHeader, input.LineItems);

                return new JsonResult(new { success = true, invoiceId = invoiceHeader.InvoiceID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing invoice");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        private async Task GenerateInvoicePdf(InvoiceHeader header, List<InvoiceLineItem> lineItems)
        {
            // Implementation will be provided in a separate method
            await Task.CompletedTask;
        }
    }
}