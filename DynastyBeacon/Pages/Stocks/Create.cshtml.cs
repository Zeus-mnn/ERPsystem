using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynastyBeacon.Models;

namespace DynastyBeacon.Pages.Stocks
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public StockCreateModel StockInput { get; set; } = new();

        public List<string> ExistingCategories { get; set; } = new();

        [BindProperty]
        public string NewCategory { get; set; }

        public class StockCreateModel
        {
            [Required(ErrorMessage = "Description is required")]
            [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
            [Display(Name = "Description")]
            public string StockDescription { get; set; }

            [Required(ErrorMessage = "Cost is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Cost must be greater than or equal to 0")]
            [DisplayFormat(DataFormatString = "{0:C2}")]
            public decimal Cost { get; set; }

            [Required(ErrorMessage = "Selling Price is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Selling Price must be greater than or equal to 0")]
            [Display(Name = "Selling Price")]
            [DisplayFormat(DataFormatString = "{0:C2}")]
            public decimal SellingPrice { get; set; }

            [Required(ErrorMessage = "Category is required")]
            [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
            public string Category { get; set; }

            [Display(Name = "Initial Stock")]
            [Range(0, int.MaxValue, ErrorMessage = "Initial stock must be greater than or equal to 0")]
            public int StockOnHand { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get existing categories
            ExistingCategories = await _context.Stocks
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            StockInput = new StockCreateModel
            {
                Cost = 0,
                SellingPrice = 0,
                StockOnHand = 0
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // If new category is provided, use it instead of selected category
                if (!string.IsNullOrWhiteSpace(NewCategory))
                {
                    StockInput.Category = NewCategory.Trim();
                }

                if (!ModelState.IsValid)
                {
                    // Reload categories for the view
                    ExistingCategories = await _context.Stocks
                        .Select(s => s.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync();

                    return Page();
                }

                // Validate selling price is greater than cost
                if (StockInput.SellingPrice < StockInput.Cost)
                {
                    ModelState.AddModelError("StockInput.SellingPrice",
                        "Selling price must be greater than or equal to cost.");

                    // Reload categories for the view
                    ExistingCategories = await _context.Stocks
                        .Select(s => s.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync();

                    return Page();
                }

                var stock = new Stock
                {
                    StockID = Guid.NewGuid(),
                    StockDescription = StockInput.StockDescription.Trim(),
                    Cost = StockInput.Cost,
                    SellingPrice = StockInput.SellingPrice,
                    Category = StockInput.Category,
                    StockOnHand = StockInput.StockOnHand,
                    QtyPurchased = StockInput.StockOnHand,
                    QtySold = 0,
                    TotalPurchasesExclVat = StockInput.Cost * StockInput.StockOnHand,
                    TotalSalesExclVat = 0,
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                };

                _context.Stocks.Add(stock);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created new stock item. ID: {StockID}, Description: {Description}, Category: {Category}",
                    stock.StockID,
                    stock.StockDescription,
                    stock.Category);

                TempData["SuccessMessage"] = "Stock item created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock item");
                ModelState.AddModelError("", "An unexpected error occurred while creating the stock item.");

                // Reload categories for the view
                ExistingCategories = await _context.Stocks
                    .Select(s => s.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Page();
            }
        }
    }
}