using DynastyBeacon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DynastyBeacon.Pages.Enquries
{


    public class DebtorsEnquiryModel : PageModel
        {
            private readonly ApplicationDbContext _context;
            private readonly ILogger<DebtorsEnquiryModel> _logger;
            private const decimal VAT_RATE = 0.15m;

            public DebtorsEnquiryModel(ApplicationDbContext context, ILogger<DebtorsEnquiryModel> logger)
            {
                _context = context;
                _logger = logger;

                // Set South African culture
                var zaCulture = new CultureInfo("en-ZA");
                CultureInfo.DefaultThreadCurrentCulture = zaCulture;
                CultureInfo.DefaultThreadCurrentUICulture = zaCulture;
            }

            public class DebtorSummary
            {
                public Guid DebtorID { get; set; }
                public string AccountCode { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
                public decimal Balance { get; set; }
                public string Tier { get; set; }
                public DateTime? LastTransactionDate { get; set; }
                public decimal TotalTransactions { get; set; }
            }

            public class DetailedDebtorInfo
            {
                public Debtor BasicInfo { get; set; }
                public List<DebtorTransaction> Transactions { get; set; }
                public decimal TotalTransactionValue { get; set; }
                public decimal AverageTransactionValue { get; set; }
                public DebtorTransaction LargestTransaction { get; set; }
                public DebtorTransaction MostRecentTransaction { get; set; }
                public int TotalTransactions { get; set; }
                public Dictionary<string, int> TransactionsByType { get; set; }
                public Dictionary<string, decimal> ValuesByType { get; set; }
            }

            [BindProperty(SupportsGet = true)]
            public string SearchTerm { get; set; }

            [BindProperty(SupportsGet = true)]
            public string SortOrder { get; set; }

            public List<DebtorSummary> Debtors { get; set; } = new();
            public DetailedDebtorInfo SelectedDebtor { get; set; }
            public string ErrorMessage { get; set; }

            public async Task<IActionResult> OnGetAsync(Guid? debtorId)
            {
                try
                {
                    IQueryable<Debtor> query = _context.Debtors
                        .Include(d => d.DebtorTransactions);

                    if (!string.IsNullOrWhiteSpace(SearchTerm))
                    {
                        query = query.Where(d =>
                            d.Name.Contains(SearchTerm) ||
                            d.AccountCode.Contains(SearchTerm) ||
                            d.Email.Contains(SearchTerm));
                    }

                    // Get basic list of debtors
                    Debtors = await query
                        .Select(d => new DebtorSummary
                        {
                            DebtorID = d.DebtorID,
                            AccountCode = d.AccountCode,
                            Name = d.Name,
                            Email = d.Email,
                            Balance = d.Balance,
                            Tier = d.Tier,
                            LastTransactionDate = d.DebtorTransactions
                                .OrderByDescending(t => t.TransactionDate)
                                .Select(t => t.TransactionDate)
                                .FirstOrDefault(),
                            TotalTransactions = d.DebtorTransactions
                                .Sum(t => t.GrossTransactionValue)
                        })
                        .ToListAsync();

                    // Apply sorting
                    Debtors = SortOrder switch
                    {
                        "name_desc" => Debtors.OrderByDescending(d => d.Name).ToList(),
                        "name" => Debtors.OrderBy(d => d.Name).ToList(),
                        "balance_desc" => Debtors.OrderByDescending(d => d.Balance).ToList(),
                        "balance" => Debtors.OrderBy(d => d.Balance).ToList(),
                        "recent_desc" => Debtors.OrderByDescending(d => d.LastTransactionDate).ToList(),
                        "recent" => Debtors.OrderBy(d => d.LastTransactionDate).ToList(),
                        _ => Debtors.OrderBy(d => d.AccountCode).ToList()
                    };

                    // If a specific debtor is selected, get detailed information
                    if (debtorId.HasValue)
                    {
                        var debtor = await _context.Debtors
                            .Include(d => d.DebtorTransactions)
                            .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                        if (debtor != null)
                        {
                            SelectedDebtor = new DetailedDebtorInfo
                            {
                                BasicInfo = debtor,
                                Transactions = debtor.DebtorTransactions.OrderByDescending(t => t.TransactionDate).ToList(),
                                TotalTransactionValue = debtor.DebtorTransactions.Sum(t => t.GrossTransactionValue),
                                AverageTransactionValue = debtor.DebtorTransactions.Any()
                                    ? debtor.DebtorTransactions.Average(t => t.GrossTransactionValue)
                                    : 0,
                                LargestTransaction = debtor.DebtorTransactions
                                    .OrderByDescending(t => t.GrossTransactionValue)
                                    .FirstOrDefault(),
                                MostRecentTransaction = debtor.DebtorTransactions
                                    .OrderByDescending(t => t.TransactionDate)
                                    .FirstOrDefault(),
                                TotalTransactions = debtor.DebtorTransactions.Count,
                                TransactionsByType = debtor.DebtorTransactions
                                    .GroupBy(t => t.TransactionType)
                                    .ToDictionary(g => g.Key, g => g.Count()),
                                ValuesByType = debtor.DebtorTransactions
                                    .GroupBy(t => t.TransactionType)
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
                    .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                if (debtor == null)
                    return NotFound();

                var details = new DetailedDebtorInfo
                {
                    BasicInfo = debtor,
                    Transactions = debtor.DebtorTransactions.OrderByDescending(t => t.TransactionDate).ToList(),
                    TotalTransactionValue = debtor.DebtorTransactions.Sum(t => t.GrossTransactionValue),
                    AverageTransactionValue = debtor.DebtorTransactions.Any()
                        ? debtor.DebtorTransactions.Average(t => t.GrossTransactionValue)
                        : 0,
                    LargestTransaction = debtor.DebtorTransactions
                        .OrderByDescending(t => t.GrossTransactionValue)
                        .FirstOrDefault(),
                    MostRecentTransaction = debtor.DebtorTransactions
                        .OrderByDescending(t => t.TransactionDate)
                        .FirstOrDefault(),
                    TotalTransactions = debtor.DebtorTransactions.Count,
                    TransactionsByType = debtor.DebtorTransactions
                        .GroupBy(t => t.TransactionType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ValuesByType = debtor.DebtorTransactions
                        .GroupBy(t => t.TransactionType)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.GrossTransactionValue))
                };

                return new JsonResult(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving debtor details for ID: {DebtorId}", debtorId);
                return StatusCode(500, "Error retrieving debtor details");
            }
        }
    }
    }