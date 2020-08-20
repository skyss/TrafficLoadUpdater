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
        public int DisplayPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int Size { get; set; } = 50;

        [BindProperty]
        public String StopName { get; set; }

        [BindProperty]
        public String StopName_text { get; set; }

        [BindProperty]
        public String TimeSpan { get; set; } = $"[{((long)DateTime.Now.TimeOfDay.TotalMinutes).ToString()}, {(((long) DateTime.Now.TimeOfDay.TotalMinutes) + 180).ToString()}]";

        public DateTime TimeSpanFrom { 
            get {
                var hours = TimeSpan.Replace("[", "").Replace("]", "").Split(",");
                return Date.Date.AddMinutes(double.Parse(hours[0]));
            }
        }

        public DateTime TimeSpanTo {  
            get
            {
                var hours = TimeSpan.Replace("[", "").Replace("]", "").Split(",");
                return Date.Date.AddMinutes(double.Parse(hours[1]));
            }
        }

        [BindProperty(SupportsGet = true)]
        public TrafficLightStatus Status { get; set; } = TrafficLightStatus.Yellow;

        [BindProperty(SupportsGet = true)]
        public TrafficLightStatus FilterStatus { get; set; } = TrafficLightStatus.Green;

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = DateTime.Now.AddDays(-1);

        [BindProperty(SupportsGet = true)]
        public String Line { get; set; }

        public PagedResult<TurModel> TurModel { get; set; }

        private readonly TrafficLoadContext _context;

        public IndexModel(TrafficLoadContext context)
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
                .Include(h => h.History)
                .Where(t => t.AvgangsTid >= TimeSpanFrom && t.AvgangsTid <= TimeSpanTo);

            if (!String.IsNullOrWhiteSpace(StopName))
            {
                var sql = @$"SELECT 
                                [o].[TripKey], [o].[AvgangsStopp], [o].[FraStoppG], [o].[FraStoppR], [o].[FraStoppY], [o].[FraTidG], [o].[FraTidR], [o].[FraTidY], [o].[Kapasitet], [o].[LineName], [o].[Ombord], [o].[Paastigende], [o].[RouteFromToKey], [o].[RuteNamn], [o].[TilTidG], [o].[TilTidR], [o].[TilTidY], [o].[TripStatus], 
	                            parse(concat(substring(d.Actual_Datekey, 1, 4), '.', substring(d.Actual_Datekey, 5, 2), '.', substring(d.Actual_Datekey, 7, 2), ' ', substring(d.TimeKey, 1, 2), ':', substring(d.TimeKey, 3, 2)) as datetime) as avgangsTid
                           FROM
                                OverlastTurer o inner join STOPPOINT_DATA_short d on o.tripkey = d.TripKey and d.StopKey in ({StopName}) and d.StopKey_to not in ({StopName})";

                query = _context.Turer.FromSqlRaw(sql)
                    .Include(h => h.History)
                    .Where(t => t.AvgangsTid >= TimeSpanFrom && t.AvgangsTid <= TimeSpanTo);
            }

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

            TurModel = query.OrderBy(t => t.AvgangsTid).GetPaged<TurModel>(DisplayPage, Size);
        }

        public IActionResult OnGetSearch(string q)
        {
            var stops = _context.Stopp.Where(s => s.Name.ToLower().Contains(q.ToLower())).Select(s => new { value = s.AltStopKeys, text = s.Name }).ToList();
            return new JsonResult(stops);
        }

    }
}
