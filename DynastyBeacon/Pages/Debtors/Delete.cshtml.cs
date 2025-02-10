using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DynastyBeacon.Models;
using System.ComponentModel.DataAnnotations;

namespace DynastyBeacon.Pages.Debtors
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Debtor Debtor { get; set; } = default!;

        [BindProperty]
        [Display(Name = "Deletion Description")]
        public string DeletionDescription { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Include any related data you might need for the report
            var debtor = await _context.Debtors
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DebtorID == id);

            if (debtor == null)
            {
                return NotFound();
            }

            Debtor = debtor;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtor = await _context.Debtors.FindAsync(id);

            if (debtor == null)
            {
                return NotFound();
            }

            try
            {
                // Log the deletion for audit purposes
                _logger.LogInformation(
                    "Debtor deletion initiated - ID: {DebtorId}, Name: {DebtorName}, Description: {Description}",
                    debtor.DebtorID,
                    debtor.Name,
                    DeletionDescription);

                Debtor = debtor;
                _context.Debtors.Remove(Debtor);
                await _context.SaveChangesAsync();

                StatusMessage = "Debtor was successfully deleted.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting debtor {DebtorId}", id);
                StatusMessage = "Error: Failed to delete debtor. Please try again.";
                return RedirectToPage("./Delete", new { id });
            }
        }

        // New method to generate report content (could be used for API endpoint if needed)
        private string GenerateAuditReport()
        {
            return $@"
DEBTOR DELETION AUDIT REPORT
============================
Generated on: {DateTime.Now}

BASIC INFORMATION
----------------
Account Code: {Debtor.AccountCode}
Name: {Debtor.Name}
Phone: {Debtor.Phone}
Email: {Debtor.Email}

FINANCIAL STATUS
---------------
Current Balance: R {Debtor.Balance:N2}
Total Sales YTD: R {Debtor.SalesYearToDate:N2}
Tier: {Debtor.Tier}

DELETION DETAILS
---------------
Deletion Date: {DateTime.Now}
Description: {DeletionDescription}
Deleted By: {User.Identity?.Name ?? "Unknown User"}
";
        }

        // Optional: API endpoint to get report content
        public IActionResult OnGetGenerateReport(Guid id)
        {
            if (Debtor == null || Debtor.DebtorID != id)
            {
                return NotFound();
            }

            var reportContent = GenerateAuditReport();
            var fileName = $"debtor_deletion_report_{Debtor.AccountCode}_{DateTime.Now:yyyy-MM-dd}.txt";

            return File(System.Text.Encoding.UTF8.GetBytes(reportContent),
                       "text/plain",
                       fileName);
        }
    }
}