using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DynastyBeacon.Pages.Stocks
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public StockEditModel StockInput { get; set; } = new();

        // Read-only display properties
        public string StockCode { get; set; }
        public string Category { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public class StockEditModel
        {
            [Required]
            public Guid StockID { get; set; }

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

            // Remaining properties as read-only
            public int StockOnHand { get; set; }
            public int QtyPurchased { get; set; }
            public int QtySold { get; set; }
            public decimal TotalPurchasesExclVat { get; set; }
            public decimal TotalSalesExclVat { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.Stocks.FirstOrDefaultAsync(m => m.StockID == id);
            if (stock == null)
            {
                return NotFound();
            }

            // Map Stock entity to StockEditModel
            StockInput = new StockEditModel
            {
                StockID = stock.StockID,
                StockDescription = stock.StockDescription,
                Cost = stock.Cost,
                SellingPrice = stock.SellingPrice,
                StockOnHand = stock.StockOnHand,
                QtyPurchased = stock.QtyPurchased,
                QtySold = stock.QtySold,
                TotalPurchasesExclVat = stock.TotalPurchasesExclVat,
                TotalSalesExclVat = stock.TotalSalesExclVat
            };

            // Set read-only properties
            StockCode = stock.StockCode;
            Category = stock.Category;
            CreatedBy = stock.CreatedBy;
            CreatedOn = stock.CreatedOn;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var stock = await _context.Stocks.FindAsync(StockInput.StockID);
                if (stock == null)
                {
                    return NotFound();
                }

                // Validate selling price against cost
                if (StockInput.SellingPrice < StockInput.Cost)
                {
                    ModelState.AddModelError("StockInput.SellingPrice",
                        "Selling price must be greater than or equal to cost.");
                    return Page();
                }

                // Update only editable fields
                stock.StockDescription = StockInput.StockDescription.Trim();
                stock.Cost = StockInput.Cost;
                stock.SellingPrice = StockInput.SellingPrice;

                // Update audit fields
                stock.UpdatedBy = "System";
                stock.UpdatedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated stock item. ID: {StockID}, Description: {Description}",
                    stock.StockID,
                    stock.StockDescription);

                TempData["SuccessMessage"] = "Stock item updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock item");
                ModelState.AddModelError("", "An unexpected error occurred while updating the stock item.");
                return Page();
            }
        }

        private async Task<bool> StockExists(Guid id)
        {
            return await _context.Stocks.AnyAsync(e => e.StockID == id);
        }
    }
}