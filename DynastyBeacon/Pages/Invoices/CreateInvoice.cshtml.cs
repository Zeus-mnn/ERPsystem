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
        private const decimal VAT_RATE = 0.15m;

        public CreateInvoiceModel(ApplicationDbContext context, ILogger<CreateInvoiceModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InvoiceInputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public Debtor? SelectedDebtor { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<JsonResult> OnGetSearchDebtorsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new JsonResult(Array.Empty<object>());

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
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new JsonResult(Array.Empty<object>());

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

            if (debtor == null)
                return new JsonResult(new { error = "Debtor not found" });

            return new JsonResult(debtor);
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostProcessInvoiceAsync([FromBody] InvoiceInputModel model)
        {
            try
            {
                // Clear irrelevant validation errors
                ModelState.Remove("SearchTerm");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogError("Invalid model state: {Errors}", errors);
                    return BadRequest(new { error = errors });
                }

                if (model.LineItems == null || !model.LineItems.Any())
                {
                    return BadRequest(new { error = "At least one line item is required" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get stock items and validate
                    var stockIds = model.LineItems.Select(x => x.StockID).ToList();
                    var stocks = await _context.Stocks
                        .Where(s => stockIds.Contains(s.StockID))
                        .ToDictionaryAsync(s => s.StockID, s => s);

                    // Validate all stocks exist and have sufficient quantity
                    foreach (var item in model.LineItems)
                    {
                        if (!stocks.TryGetValue(item.StockID, out var stock))
                        {
                            return BadRequest(new { error = $"Stock not found: {item.StockID}" });
                        }

                        if (stock.StockOnHand < item.Quantity)
                        {
                            return BadRequest(new { error = $"Insufficient stock for {stock.StockDescription}" });
                        }
                    }

                    // Create Invoice Header
                    var invoiceHeader = new InvoiceHeader
                    {
                        InvoiceID = Guid.NewGuid(),
                        DebtorID = model.DebtorID,
                        InvoiceDate = model.InvoiceDate,
                        TotalSellAmountExclVAT = model.LineItems.Sum(x => (x.Quantity * x.UnitPrice) - x.Discount),
                        VAT = model.LineItems.Sum(x => ((x.Quantity * x.UnitPrice) - x.Discount) * VAT_RATE),
                        TotalCost = model.LineItems.Sum(x => x.Quantity * stocks[x.StockID].Cost)
                    };

                    _context.InvoiceHeaders.Add(invoiceHeader);
                    await _context.SaveChangesAsync(); // Save to generate the ID and computed InvoiceNo

                    // Get the generated InvoiceNo for transactions
                    await _context.Entry(invoiceHeader).ReloadAsync(); // Reload to get computed columns
                    var generatedInvoiceNo = invoiceHeader.InvoiceNo;

                    // Create Invoice Details
                    foreach (var item in model.LineItems)
                    {
                        var stock = stocks[item.StockID];
                        var detail = new InvoiceDetail
                        {
                            InvoiceDetailID = Guid.NewGuid(),
                            InvoiceID = invoiceHeader.InvoiceID,
                            StockID = item.StockID,
                            QtySold = item.Quantity,
                            UnitCost = stock.Cost,
                            UnitSell = item.UnitPrice,
                            Disc = item.Discount,
                            Total = (item.Quantity * item.UnitPrice) - item.Discount
                        };

                        _context.InvoiceDetails.Add(detail);

                        // Update Stock
                        stock.StockOnHand -= item.Quantity;
                        stock.QtySold += item.Quantity;
                        stock.TotalSalesExclVat += detail.Total;
                        stock.UpdatedBy = "System";
                        stock.UpdatedOn = DateTime.Now;
                    }

                    // Create Stock Transactions
                    foreach (var item in model.LineItems)
                    {
                        var stock = stocks[item.StockID];
                        var stockTransaction = new StockTransaction
                        {
                            TransactionID = Guid.NewGuid(),
                            StockID = item.StockID,
                            TransactionDate = DateTime.Now,
                            TransactionType = "SALE",
                            DocumentNo = generatedInvoiceNo,
                            Qty = item.Quantity,
                            UnitCost = stock.Cost,
                            UnitSell = item.UnitPrice
                            
                        };

                        _context.StockTransactions.Add(stockTransaction);
                    }

                    // Update Debtor
                    var debtor = await _context.Debtors.FindAsync(model.DebtorID);
                    if (debtor == null)
                    {
                        throw new Exception("Debtor not found");
                    }

                    debtor.Balance += invoiceHeader.TotalSellAmountExclVAT + invoiceHeader.VAT;
                    debtor.SalesYearToDate += invoiceHeader.TotalSellAmountExclVAT;
                    debtor.CostYearToDate += invoiceHeader.TotalCost;
                    debtor.UpdatedBy = "System";
                    debtor.UpdatedOn = DateTime.Now;

                    // Create Debtor Transaction
                    var debtorTransaction = new DebtorTransaction
                    {
                        TransactionID = Guid.NewGuid(),
                        DebtorID = model.DebtorID,
                        TransactionDate = DateTime.Now,
                        TransactionType = "SALE",
                        DocumentNo = generatedInvoiceNo,
                        GrossTransactionValue = invoiceHeader.TotalSellAmountExclVAT + invoiceHeader.VAT,
                        VatValue = invoiceHeader.VAT
                        
                    };

                    _context.DebtorTransactions.Add(debtorTransaction);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Invoice {InvoiceNo} created successfully", generatedInvoiceNo);

                    return new JsonResult(new
                    {
                        success = true,
                        invoiceId = invoiceHeader.InvoiceID,
                        invoiceNo = generatedInvoiceNo
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing invoice for Debtor {DebtorID}", model.DebtorID);
                    return BadRequest(new { error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in invoice processing");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }
    }

    public class InvoiceInputModel
    {
        [Required(ErrorMessage = "Debtor selection is required")]
        public Guid DebtorID { get; set; }

        [Required(ErrorMessage = "Invoice date is required")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "At least one line item is required")]
        public List<InvoiceLineItemInput> LineItems { get; set; } = new();
    }

    public class InvoiceLineItemInput
    {
        [Required(ErrorMessage = "Stock selection is required")]
        public Guid StockID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative")]
        public decimal Discount { get; set; }
    }
}