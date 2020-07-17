using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using TrafficLoadWeb.Data;
using TrafficLoadWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TrafficLoadWeb.Pages
{
 
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public TrafficLightStatus Status { get; set; } = TrafficLightStatus.Yellow;

        [BindProperty(SupportsGet = true)]
        public TrafficLightStatus FilterStatus { get; set; } = TrafficLightStatus.Green;

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = DateTime.Now.AddDays(-1);

        [BindProperty(SupportsGet = true)]
        public String Line { get; set; }

        public IList<TurModel> TurModel { get; set; }

        private readonly TrafficLoadContext _context;

        public IndexModel(TrafficLoadContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            var query = _context.Turer
                .Include(h => h.History)
                .Where(t => t.AvgangsTid.Date == Date.Date);

            if (!String.IsNullOrEmpty(Line))
                query = query.Where(t => t.LineName.Equals(Line));

            if (FilterStatus == TrafficLightStatus.Yellow)
            {
                decimal factor = Convert.ToDecimal((int)Status / 100.0);
                query = query.Where(t => t.TripStatus == 1);
                query = query.Where(t => t.Ombord > ((decimal)t.Kapasitet * factor));
            }

            if (FilterStatus == TrafficLightStatus.Red)
            {
                decimal factor = Convert.ToDecimal((int)Status / 100.0);

                query = query.Where(t => t.TripStatus == 1);
                if (Status == TrafficLightStatus.Green)
                    query = query.Where(t => t.Ombord > (((decimal)t.Kapasitet * factor) * Convert.ToDecimal(1.5)) || (t.TilTidG - t.FraTidG).Value.Minutes >= 15);
                else if (Status == TrafficLightStatus.Yellow)
                    query = query.Where(t => t.Ombord > (((decimal)t.Kapasitet * factor) * Convert.ToDecimal(1.5)) || (t.TilTidY - t.FraTidY).Value.Minutes >= 15);
                else 
                    query = query.Where(t => t.Ombord > (((decimal)t.Kapasitet * factor) * Convert.ToDecimal(1.5)) || (t.TilTidR - t.FraTidR).Value.Minutes >= 15);
            }

            TurModel = await query.OrderBy(t => t.AvgangsTid).ToListAsync<TurModel>();
        }

    }
}
