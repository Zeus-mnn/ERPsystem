using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using DynastyBeacon.Models;

namespace DynastyBeacon.Pages.Debtors
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
        public DebtorEditModel DebtorInput { get; set; } = new();

        public class DebtorEditModel
        {
            [Required]
            public Guid DebtorID { get; set; }

            [Display(Name = "Account Code")]
            public string AccountCode { get; set; }

            [Required(ErrorMessage = "Name is required")]
            [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Address is required")]
            [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
            public string Address { get; set; }

            [StringLength(255, ErrorMessage = "Alternative Address cannot exceed 255 characters")]
            [Display(Name = "Alternative Address")]
            public string? AlternativeAddress { get; set; }

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number format")]
            [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address format")]
            [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Balance is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Balance must be greater than or equal to 0")]
            public decimal Balance { get; set; }

            [Display(Name = "Sales Year To Date")]
            [Range(0, double.MaxValue)]
            public decimal SalesYearToDate { get; set; }

            [Display(Name = "Cost Year To Date")]
            [Range(0, double.MaxValue)]
            public decimal CostYearToDate { get; set; }

            [StringLength(50, ErrorMessage = "Tier cannot exceed 50 characters")]
            public string? Tier { get; set; }

            // Read-only properties for display
            [Display(Name = "Created By")]
            public string CreatedBy { get; set; }

            [Display(Name = "Created On")]
            public DateTime CreatedOn { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtor = await _context.Debtors.FirstOrDefaultAsync(m => m.DebtorID == id);
            if (debtor == null)
            {
                return NotFound();
            }

            // Map Debtor entity to DebtorEditModel
            DebtorInput = new DebtorEditModel
            {
                DebtorID = debtor.DebtorID,
                AccountCode = debtor.AccountCode, // Read-only, just for display
                Name = debtor.Name,
                Address = debtor.Address,
                AlternativeAddress = debtor.AlternativeAddress,
                Phone = debtor.Phone,
                Email = debtor.Email,
                Balance = debtor.Balance,
                SalesYearToDate = debtor.SalesYearToDate,
                CostYearToDate = debtor.CostYearToDate,
                Tier = debtor.Tier,
                CreatedBy = debtor.CreatedBy,
                CreatedOn = debtor.CreatedOn
            };

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

                var debtor = await _context.Debtors.FindAsync(DebtorInput.DebtorID);
                if (debtor == null)
                {
                    return NotFound();
                }

                // Check if email is changed and if new email already exists
                if (debtor.Email != DebtorInput.Email.Trim().ToLower())
                {
                    var emailExists = await _context.Debtors
                        .AnyAsync(d => d.Email == DebtorInput.Email.Trim().ToLower()
                                     && d.DebtorID != DebtorInput.DebtorID);
                    if (emailExists)
                    {
                        ModelState.AddModelError("DebtorInput.Email", "This email address is already registered.");
                        return Page();
                    }
                }

                // Update only the editable fields
                debtor.Name = DebtorInput.Name.Trim();
                debtor.Address = DebtorInput.Address.Trim();
                debtor.AlternativeAddress = DebtorInput.AlternativeAddress?.Trim();
                debtor.Phone = DebtorInput.Phone.Trim();
                debtor.Email = DebtorInput.Email.Trim().ToLower();
                debtor.Balance = DebtorInput.Balance;
                debtor.SalesYearToDate = DebtorInput.SalesYearToDate;
                debtor.CostYearToDate = DebtorInput.CostYearToDate;
                debtor.Tier = DebtorInput.Tier?.Trim();

                // Update audit fields
                debtor.UpdatedBy = "System";
                debtor.UpdatedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated debtor. ID: {DebtorID}, Name: {Name}",
                    debtor.DebtorID,
                    debtor.Name);

                TempData["SuccessMessage"] = "Debtor updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error when updating debtor {DebtorID}", DebtorInput.DebtorID);

                if (!await DebtorExists(DebtorInput.DebtorID))
                {
                    return NotFound();
                }

                ModelState.AddModelError("",
                    "The record was modified by another user. Please refresh and try again.");
                return Page();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error when updating debtor {DebtorID}", DebtorInput.DebtorID);
                ModelState.AddModelError("",
                    "Unable to save changes. Please try again, and if the problem persists, " +
                    "contact your system administrator.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when updating debtor {DebtorID}", DebtorInput.DebtorID);
                ModelState.AddModelError("", "An unexpected error occurred while updating the debtor.");
                return Page();
            }
        }

        private async Task<bool> DebtorExists(Guid id)
        {
            return await _context.Debtors.AnyAsync(e => e.DebtorID == id);
        }
    }
}