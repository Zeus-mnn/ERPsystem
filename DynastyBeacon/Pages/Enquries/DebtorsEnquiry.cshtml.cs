using DynastyBeacon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DynastyBeacon.Pages.Enquries
{
    public class DebtorsEnquiryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DebtorsEnquiryModel> _logger;
        private const decimal VAT_RATE = 0.15m;

        public DebtorsEnquiryModel(ApplicationDbContext context, ILogger<DebtorsEnquiryModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set South African culture
            var zaCulture = new CultureInfo("en-ZA");
            CultureInfo.DefaultThreadCurrentCulture = zaCulture;
            CultureInfo.DefaultThreadCurrentUICulture = zaCulture;
        }

        public class DebtorSummary
        {
            public Guid DebtorID { get; set; }
            public string AccountCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public decimal Balance { get; set; }
            public string Tier { get; set; } = string.Empty;
            public DateTime? LastTransactionDate { get; set; }
            public decimal TotalTransactions { get; set; }
        }

        public class DetailedDebtorInfo
        {
            public Debtor BasicInfo { get; set; }
            public List<DebtorTransaction> Transactions { get; set; } = new();
            public decimal TotalTransactionValue { get; set; }
            public decimal AverageTransactionValue { get; set; }
            public DebtorTransaction? LargestTransaction { get; set; }
            public DebtorTransaction? MostRecentTransaction { get; set; }
            public int TotalTransactions { get; set; }
            public Dictionary<string, int> TransactionsByType { get; set; } = new();
            public Dictionary<string, decimal> ValuesByType { get; set; } = new();
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public List<DebtorSummary> Debtors { get; set; } = new();
        public DetailedDebtorInfo? SelectedDebtor { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? debtorId)
        {
            try
            {
                IQueryable<Debtor> query = _context.Debtors
                    .Include(d => d.DebtorTransactions)
                    .AsNoTracking();  // Add this for better performance when reading

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    query = query.Where(d =>
                        d.Name.Contains(SearchTerm) ||
                        d.AccountCode.Contains(SearchTerm) ||
                        (d.Email != null && d.Email.Contains(SearchTerm)));
                }

                // Get basic list of debtors with error handling
                try
                {
                    Debtors = await query
                        .Select(d => new DebtorSummary
                        {
                            DebtorID = d.DebtorID,
                            AccountCode = d.AccountCode ?? string.Empty,
                            Name = d.Name ?? string.Empty,
                            Email = d.Email ?? string.Empty,
                            Balance = d.Balance,
                            Tier = d.Tier ?? "Standard",
                            LastTransactionDate = d.DebtorTransactions
                                .OrderByDescending(t => t.TransactionDate)
                                .Select(t => t.TransactionDate)
                                .FirstOrDefault(),
                            TotalTransactions = d.DebtorTransactions
                                .Sum(t => t.GrossTransactionValue)
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving debtor list");
                    ErrorMessage = "Error retrieving debtor list. Please try again.";
                    return Page();
                }

                // Apply sorting with null checks
                Debtors = SortOrder switch
                {
                    "name_desc" => Debtors.OrderByDescending(d => d.Name).ToList(),
                    "name" => Debtors.OrderBy(d => d.Name).ToList(),
                    "balance_desc" => Debtors.OrderByDescending(d => d.Balance).ToList(),
                    "balance" => Debtors.OrderBy(d => d.Balance).ToList(),
                    "recent_desc" => Debtors.OrderByDescending(d => d.LastTransactionDate ?? DateTime.MinValue).ToList(),
                    "recent" => Debtors.OrderBy(d => d.LastTransactionDate ?? DateTime.MinValue).ToList(),
                    _ => Debtors.OrderBy(d => d.AccountCode).ToList()
                };

                // If a specific debtor is selected, get detailed information
                if (debtorId.HasValue)
                {
                    var debtor = await _context.Debtors
                        .Include(d => d.DebtorTransactions)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                    if (debtor != null)
                    {
                        var transactions = debtor.DebtorTransactions?.ToList() ?? new List<DebtorTransaction>();

                        SelectedDebtor = new DetailedDebtorInfo
                        {
                            BasicInfo = debtor,
                            Transactions = transactions.OrderByDescending(t => t.TransactionDate).ToList(),
                            TotalTransactionValue = transactions.Sum(t => t.GrossTransactionValue),
                            AverageTransactionValue = transactions.Any()
                                ? transactions.Average(t => t.GrossTransactionValue)
                                : 0,
                            LargestTransaction = transactions
                                .OrderByDescending(t => t.GrossTransactionValue)
                                .FirstOrDefault(),
                            MostRecentTransaction = transactions
                                .OrderByDescending(t => t.TransactionDate)
                                .FirstOrDefault(),
                            TotalTransactions = transactions.Count,
                            TransactionsByType = transactions
                                .GroupBy(t => t.TransactionType ?? "Unknown")
                                .ToDictionary(g => g.Key, g => g.Count()),
                            ValuesByType = transactions
                                .GroupBy(t => t.TransactionType ?? "Unknown")
                                .ToDictionary(g => g.Key, g => g.Sum(t => t.GrossTransactionValue))
                        };
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Debtors Enquiry");
                ErrorMessage = "An error occurred while loading debtor information.";
                return Page();
            }
        }

        public string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2", new CultureInfo("en-ZA"));
        }

        public async Task<IActionResult> OnGetDebtorDetailsAsync(Guid debtorId)
        {
            try
            {
                var debtor = await _context.Debtors
                    .Include(d => d.DebtorTransactions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                if (debtor == null)
                {
                    _logger.LogWarning("Debtor not found: {DebtorId}", debtorId);
                    return NotFound(new { message = "Debtor not found" });
                }

                // Map to a simpler DTO structure
                var debtorDetails = new
                {
                    BasicInfo = new
                    {
                        debtor.DebtorID,
                        debtor.AccountCode,
                        debtor.Name,
                        debtor.Email,
                        debtor.Address,
                        debtor.AlternativeAddress,
                        debtor.Phone,
                        debtor.Balance,
                        debtor.SalesYearToDate,
                        debtor.CostYearToDate,
                        debtor.Tier
                    },
                    Transactions = debtor.DebtorTransactions?.Select(t => new
                    {
                        t.TransactionID,
                        t.TransactionCode,
                        t.TransactionDate,
                        t.TransactionType,
                        t.DocumentNo,
                        t.GrossTransactionValue,
                        t.VatValue
                    }).OrderByDescending(t => t.TransactionDate).ToList(),
                    TotalTransactionValue = debtor.DebtorTransactions?.Sum(t => t.GrossTransactionValue) ?? 0,
                    AverageTransactionValue = debtor.DebtorTransactions?.Any() == true
                        ? debtor.DebtorTransactions.Average(t => t.GrossTransactionValue)
                        : 0,
                    TotalTransactions = debtor.DebtorTransactions?.Count ?? 0,
                    TransactionsByType = debtor.DebtorTransactions?
                        .GroupBy(t => t.TransactionType ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<string, int>(),
                    ValuesByType = debtor.DebtorTransactions?
                        .GroupBy(t => t.TransactionType ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.GrossTransactionValue)) ?? new Dictionary<string, decimal>()
                };

                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return new JsonResult(debtorDetails, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving debtor details for ID: {DebtorId}", debtorId);
                return StatusCode(500, new { message = "An error occurred while retrieving debtor details" });
            }
        }
    }
}