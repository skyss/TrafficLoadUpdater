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
    public class OverlastStatus
    {
        public String LineName { get; set; }
        public String Avgang { get; set; }
        public String Fra { get; set; }
        public int Status { get; set; }
        public String DagEinOmbord { get; set; }
        public String DagEinTid { get; set; }
        public String DagEinFraStopp { get; set; }
        public String DagEinKapasitet { get; set; }
        public int DagEinStatus { get; set; }
        public String DagToOmbord { get; set; }
        public String DagToTid { get; set; }
        public String DagToFraStopp { get; set; }
        public String DagToKapasitet { get; set; }
        public int DagToStatus { get; set; }
        public String DagTreOmbord { get; set; }
        public String DagTreTid { get; set; }
        public String DagTreFraStopp { get; set; }
        public String DagTreKapasitet { get; set; }
        public int DagTreStatus { get; set; }
        public String DagFireOmbord { get; set; }
        public String DagFireTid { get; set; }
        public String DagFireFraStopp { get; set; }
        public String DagFireKapasitet { get; set; }
        public int DagFireStatus { get; set; }
        public String DagFemOmbord { get; set; }
        public String DagFemTid { get; set; }
        public String DagFemFraStopp { get; set; }
        public String DagFemKapasitet { get; set; }
        public int DagFemStatus { get; set; }
        public String DagSeksOmbord { get; set; }
        public String DagSeksTid { get; set; }
        public String DagSeksFraStopp { get; set; }
        public String DagSeksKapasitet { get; set; }
        public int DagSeksStatus { get; set; }
        public String DagSjuOmbord { get; set; }
        public String DagSjuTid { get; set; }
        public String DagSjuFraStopp { get; set; }
        public String DagSjuKapasitet { get; set; }
        public int DagSjuStatus { get; set; }

        public bool isRedDagEin {  get
            {
                if (String.IsNullOrEmpty(DagEinTid))
                    return false;
                if (double.Parse(DagEinOmbord) > Math.Floor(double.Parse(DagEinKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagEinTid).Minutes >= 15)
                    return true;
                return false;
            }
        }

        public bool isRedDagTo
        {
            get
            {
                if (String.IsNullOrEmpty(DagToTid))
                    return false;

                if (double.Parse(DagToOmbord) > Math.Floor(double.Parse(DagToKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagToTid).Minutes >= 15)
                    return true;
                return false;
            }
        }
        public bool isRedDagTre
        {
            get
            {
                if (String.IsNullOrEmpty(DagTreTid))
                    return false;
                if (double.Parse(DagTreOmbord) > Math.Floor(double.Parse(DagTreKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagTreTid).Minutes >= 15)
                    return true;
                return false;
            }
        }
        public bool isRedDagFire
        {
            get
            {
                if (String.IsNullOrEmpty(DagFireTid))
                    return false;
                if (double.Parse(DagFireOmbord) > Math.Floor(double.Parse(DagFireKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagFireTid).Minutes >= 15)
                    return true;
                return false;
            }
        }
        public bool isRedDagFem
        {
            get
            {
                if (String.IsNullOrEmpty(DagFemTid))
                    return false;

                if (double.Parse(DagFemOmbord) > Math.Floor(double.Parse(DagFemKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagFemTid).Minutes >= 15)
                    return true;
                return false;
            }
        }
        public bool isRedDagSeks
        {
            get
            {
                if (String.IsNullOrEmpty(DagSeksTid))
                    return false;

                if (double.Parse(DagSeksOmbord) > Math.Floor(double.Parse(DagSeksKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagSeksTid).Minutes >= 15)
                    return true;
                return false;
            }
        }
        public bool isRedDagSju
        {
            get
            {
                if (String.IsNullOrEmpty(DagSjuTid))
                    return false;

                if (double.Parse(DagSjuOmbord) > Math.Floor(double.Parse(DagSjuKapasitet) * 1.5))
                    return true;
                else if (TimeSpan.Parse(DagSjuTid).Minutes >= 15)
                    return true;
                return false;
            }
        }

    }

    public class IndexModel : PageModel
    {
        //private readonly ILogger<IndexModel> _logger;
        //public IConfiguration Configuration { get; }

        //public List<OverlastStatus> Status;

        //public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //    Configuration = configuration;
        //}
        private readonly TrafficLoadContext _context;

        public IndexModel(TrafficLoadContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public String LineFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = DateTime.Now.AddDays(-6);

        public IList<TurModel> TurModel { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Turer
                .Include(h => h.History)
                .Where(t => t.AvgangsTid.Date == Date.Date);

            if (!String.IsNullOrEmpty(LineFilter))
                query = query.Where(t => t.LineName.Equals(LineFilter));

            TurModel = await query.ToListAsync<TurModel>();
        }

    }
}
