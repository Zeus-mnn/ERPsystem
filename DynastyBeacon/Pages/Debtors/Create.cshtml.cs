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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class DebtorCreateModel
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
            [Display(Name = "Name")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Address is required")]
            [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
            [Display(Name = "Address")]
            public string Address { get; set; }

            [StringLength(255, ErrorMessage = "Alternative Address cannot exceed 255 characters")]
            [Display(Name = "Alternative Address")]
            public string? AlternativeAddress { get; set; }

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number format")]
            [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
            [Display(Name = "Phone")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address format")]
            [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Balance is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Balance must be greater than or equal to 0")]
            [Display(Name = "Balance")]
            public decimal Balance { get; set; }

            [StringLength(50, ErrorMessage = "Tier cannot exceed 50 characters")]
            [Display(Name = "Tier")]
            public string? Tier { get; set; }

            public string CreatedBy { get; set; } = "System";
            public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        }

        [BindProperty]
        public DebtorCreateModel DebtorInput { get; set; } = new();

        public IActionResult OnGet()
        {
            // Initialize with default values
            DebtorInput = new DebtorCreateModel
            {
                CreatedBy = "System",
                CreatedOn = DateTime.UtcNow,
                Balance = 0
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Ensure system fields are set
                DebtorInput.CreatedBy = "System";
                DebtorInput.CreatedOn = DateTime.UtcNow;

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when creating debtor");
                    foreach (var modelStateEntry in ModelState.Values)
                    {
                        foreach (var error in modelStateEntry.Errors)
                        {
                            _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                        }
                    }
                    return Page();
                }

                // Check for duplicate email
                var emailExists = await _context.Debtors
                    .AnyAsync(d => d.Email == DebtorInput.Email.Trim().ToLower());

                if (emailExists)
                {
                    ModelState.AddModelError("DebtorInput.Email", "This email address is already registered.");
                    return Page();
                }

                var debtor = new Debtor
                {
                    DebtorID = Guid.NewGuid(),
                    Name = DebtorInput.Name?.Trim(),
                    Address = DebtorInput.Address?.Trim(),
                    AlternativeAddress = DebtorInput.AlternativeAddress?.Trim(),
                    Phone = DebtorInput.Phone?.Trim(),
                    Email = DebtorInput.Email?.Trim().ToLower(),
                    Balance = DebtorInput.Balance,
                    SalesYearToDate = 0,
                    CostYearToDate = 0,
                    Tier = DebtorInput.Tier?.Trim(),
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                };

                // Note: Do not set ID or AccountCode as they are handled by the database

                _context.Debtors.Add(debtor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created debtor. ID: {DebtorID}, Name: {Name}",
                    debtor.DebtorID, debtor.Name);

                TempData["SuccessMessage"] = "Debtor created successfully.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error when creating debtor. Data: {@DebtorInput}", DebtorInput);
                ModelState.AddModelError("",
                    "Unable to save changes. Try again, and if the problem persists, " +
                    "please contact your system administrator.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when creating debtor. Data: {@DebtorInput}", DebtorInput);
                ModelState.AddModelError("", "An unexpected error occurred while creating the debtor.");
                return Page();
            }
        }
    }
}