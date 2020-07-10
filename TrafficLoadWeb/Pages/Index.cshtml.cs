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

        private readonly TrafficLoadContext _context;
        private readonly ITurModelHelper _helper;

        public IndexModel(TrafficLoadContext context, ITurModelHelper helper)
        {
            _context = context;
            _helper = helper;
        }

        [BindProperty(SupportsGet = true)]
        public String LineFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public String WarningLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; } = DateTime.Now.AddDays(-1);

        public IList<TurModel> TurModel { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Turer
                .Include(h => h.History)
                .Where(t => t.AvgangsTid.Date == Date.Date);

            if (!String.IsNullOrEmpty(LineFilter))
                query = query.Where(t => t.LineName.Equals(LineFilter));

            TurModel = await query.ToListAsync<TurModel>();

            foreach(var m in TurModel)
                m.Helper = _helper; 

        }

    }
}
