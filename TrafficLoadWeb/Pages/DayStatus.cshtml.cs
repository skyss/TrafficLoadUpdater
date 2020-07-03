using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TrafficLoadWeb.Data;
using TrafficLoadWeb.Models;

namespace TrafficLoadWeb.Pages
{
    public class DayStatusModel : PageModel
    {
        private readonly TrafficLoadWeb.Data.TrafficLoadContext _context;

        public DayStatusModel(TrafficLoadWeb.Data.TrafficLoadContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = DateTime.Now.AddDays(-1);

        public IList<TurModel> TurModel { get;set; }

        public async Task OnGetAsync()
        {
            TurModel = await _context.Turer
                .Where(t => t.AvgangsTid.Date == Date.Date)
                .Where(t => t.Ombord > t.Kapasitet)
                .OrderBy(t => t.AvgangsTid)
                .ToListAsync<TurModel>();
        }
    }
}
