
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DynastyBeacon.Models;

namespace DynastyBeacon.Pages.Debtors
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Debtor> Debtor { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Debtor = await _context.Debtors.ToListAsync();
        }
    }
}
