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
 
    public class ReportModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = ((long)DateTime.Now.TimeOfDay.TotalMinutes) >= 377 ? DateTime.Now.AddDays(-1) : DateTime.Now.AddDays(-2);

        [BindProperty(SupportsGet = true)]
        public TrafficLightStatus Status { get; set; } = TrafficLightStatus.Yellow;

        public List<TurModel> TurModel { get; set; }

        private readonly TrafficLoadContext _context;

        public ReportModel(TrafficLoadContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            OnPost();
        }

        public void OnPost()
        {
            var query = _context.Turer
                .Include(h => h.Report)
                .Where(t => t.AvgangsTid.Date == Date.Date );

            decimal factor = Convert.ToDecimal((int)Status / 100.0);
            query = query.Where(t => t.TripStatus == 1);
            query = query.Where(t => t.Ombord > ((decimal)t.Kapasitet * factor));

            TurModel = query.OrderBy(t => t.AvgangsTid).ToList();
        }


    }
}
