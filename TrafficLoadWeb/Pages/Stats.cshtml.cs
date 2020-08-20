using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrafficLoadWeb.Data;
using TrafficLoadWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TrafficLoadWeb.Pages
{
    public class StatsModel : PageModel
    {

        private readonly TrafficLoadContext _context;

        public StatsModel(TrafficLoadContext context)
        {
            _context = context;
        }

        protected dynamic StatusToday()
        {
            var rail = _context.Turer
                .Where(t => t.AvgangsTid.Date <= DateTime.Now.Date.AddDays(-1) && t.AvgangsTid.Date >= DateTime.Now.Date.AddDays(-30))
                .Where(t => t.LineName == "1")
                .GroupBy(t => t.AvgangsTid.Date)
                .Select(t =>
                    new
                    {
                        Dato = t.Key,
                        Passasjerer = t.Sum(i => i.Paastigende),
                        Turer = t.Count()
                    }).ToList();

            decimal factor = Convert.ToDecimal((int)TrafficLightStatus.Yellow / 100.0);

            var railTrafficLight = _context.Turer
                .Where(t => t.AvgangsTid.Date <= DateTime.Now.Date.AddDays(-1) && t.AvgangsTid.Date >= DateTime.Now.Date.AddDays(-30))
                .Where(t => t.LineName == "1")
                .Where(t => t.TripStatus == 1)
                .AsEnumerable()
                .GroupBy(t => t.AvgangsTid.Date)
                .Select(t =>
                    new
                    {
                        Dato = t.Key,
                        Apc = t.Count(),
                        Red = t.Count(i => i.IsRed(TrafficLightStatus.Yellow)),
                        Yellow = t.Count(i => i.IsYellow(TrafficLightStatus.Yellow)),
                    }).ToList();

            string[] lines = new string[] { "12", "10", "2", "20", "21", "25", "27", "28", "3", "300", "300e", "4", "403", "460", "460e", "4e", "5", "50e", "6", "60", "600", "600e", "604", "80", "83", "90" };

            var bus = _context.Turer
                .Where(t => t.AvgangsTid.Date <= DateTime.Now.Date.AddDays(-1) && t.AvgangsTid.Date >= DateTime.Now.Date.AddDays(-30))
                .Where(t => lines.Contains(t.LineName))
                .GroupBy(t => t.AvgangsTid.Date)
                .Select(t =>
                    new
                    {
                        Dato = t.Key,
                        Passasjerer = t.Sum(i => i.Paastigende),
                        Turer = t.Count()
                    }).ToList();

            var busTrafficLight = _context.Turer
                .Where(t => t.AvgangsTid.Date <= DateTime.Now.Date.AddDays(-1) && t.AvgangsTid.Date >= DateTime.Now.Date.AddDays(-40))
                .Where(t => lines.Contains(t.LineName))
                .Where(t => t.TripStatus == 1)
                .AsEnumerable()
                .GroupBy(t => t.AvgangsTid.Date)
                .Select(t =>
                    new
                    {
                        Dato = t.Key,
                        Apc = t.Count(),
                        Red = t.Count(i => i.IsRed(TrafficLightStatus.Yellow)),
                        Yellow = t.Count(i => i.IsYellow(TrafficLightStatus.Yellow)),
                    }).ToList();

            var rails = rail.Select(s => new
            {
                Date = s.Dato,
                Passengers = s.Passasjerer,
                Trips = s.Turer,
                Apc = railTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Apc,
                Yellow = railTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Yellow,
                Red = railTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Red
            }).ToArray();

            var busses = bus.Select(s => new
            {
                Date = s.Dato,
                Passengers = s.Passasjerer,
                Trips = s.Turer,
                Apc = busTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Apc,
                Yellow = busTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Yellow,
                Red = busTrafficLight.Where(t => t.Dato.Equals(s.Dato)).First().Red
            }).ToArray();


            return new
            {
                Rail = rails,
                Bus = busses
            };
        }

        [BindProperty]
        public String StopName { get; set; }
        [BindProperty]
        public String StopName_text { get; set; }

        public void OnPost()
        {
            String s = "";
        }


        public JsonResult OnGetTodayRail()
        {
            return new JsonResult(StatusToday());
        }
    }
}
