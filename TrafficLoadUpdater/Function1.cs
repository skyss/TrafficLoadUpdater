using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace TrafficLoadUpdater
{
    public static class UpdateTeams
    {
        [FunctionName("SendTrafficLoadUpdate")]
        public static async void Run([TimerTrigger("0 15 8 * * *", RunOnStartup =true)]TimerInfo myTimer, ILogger log)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("nb-NO");

            log.LogInformation($"SendTrafficLoadUpdate function executed at: {DateTime.Now}");
            String lookForDate = "'" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "', '" + DateTime.Now.AddDays(-8).ToString("yyyyMMdd") + "'";

            log.LogInformation($"Looking for dates {lookForDate}");

            String sql = @"
                select 
	                operating_datekey, 
	                format(round(sum(case when LineNameLong = '1' then entered_in else 0 end), 0), '#') as Bybanen,
	                format(sum(round(case when LineNameLong != '1' then entered_in else 0 end, 0)), '#') as Buss 
                from 
	                STOPPOINT_DATA d inner join ROUTE_FROM_TO rute 
			                on d.routefromtokey = rute.RouteFromToKey
                where 
	                Operating_dateKey in (" + lookForDate + @") 
                and LineNameLong in ('1', '12', '10', '2', '20', '21', '25', '27', '28', '3', '300', '300e', '4', '403', '460', '460e', '4e', '5', '50e', '6', '60', '600', '600e', '604', '80', '83', '90')
                group by Operating_dateKey
                order by Operating_dateKey desc";

            String connectionString = config.GetConnectionString("SQLConnectionString");

            KeyValuePair<DateTime, long>[] lightRail = new KeyValuePair<DateTime, long>[2];
            KeyValuePair<DateTime, long>[] bus = new KeyValuePair<DateTime, long>[2];

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 300;
                using (var rows = await cmd.ExecuteReaderAsync())
                {
                    int i = 0;
                    while (await rows.ReadAsync())
                    {
                        DateTime d = DateTime.ParseExact(rows[0].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                        lightRail[i] = new KeyValuePair<DateTime, long>(d, long.Parse(rows[1].ToString()));
                        bus[i] = new KeyValuePair<DateTime, long>(d, long.Parse(rows[2].ToString()));
                        i++;
                        if (i > 2)
                            throw new IndexOutOfRangeException("to many rows returned from database!");
                    }
                    rows.Close();
                }
                conn.Close();
            }

            String lightRailChange = (lightRail[0].Value < lightRail[1].Value) ?
                        " (ned " + (lightRail[1].Value - lightRail[0].Value).ToString() + "/" + Math.Round((((double)lightRail[1].Value - (double)lightRail[0].Value) / (double)lightRail[0].Value) * 100, 1).ToString() + "% samanlikna med " + lightRail[1].Key.ToShortDateString() + ")"
                    :   " (opp " + (lightRail[0].Value - lightRail[1].Value).ToString() + "/" + Math.Round((((double)lightRail[0].Value - (double)lightRail[1].Value) / (double)lightRail[1].Value) * 100, 1).ToString() + "% samanlikna med  " + lightRail[1].Key.ToShortDateString() + ")";

            String busChange = (bus[0].Value < bus[1].Value) ?
                        " (ned " + (bus[1].Value - bus[0].Value).ToString() + "/" + Math.Round((((double)bus[1].Value - (double)bus[0].Value) / (double)bus[0].Value) * 100, 1).ToString() + "% samanlikna med " + bus[1].Key.ToShortDateString() + ")"
                    :   " (opp " + (bus[0].Value - bus[1].Value).ToString() + "/" + Math.Round((((double)bus[0].Value - (double)bus[1].Value) / (double)bus[1].Value) * 100, 1).ToString() + "% samanlikna med " + bus[1].Key.ToShortDateString() + ")";

            sql = @"with 
                        turer as (
	                        select
		                        d.lid, tripkey, stopkey, Entered_In, Entered_out, d.routefromtokey, stopkey_to,
		                        sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as onboard,
		                        lead(d.lid) over (partition by tripkey ORDER BY d.lid) as nesteStoppLid,
		                        first_value(stopkey) over (partition by tripkey ORDER BY d.lid) as forsteStoppId,
		                        first_value(parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime)) over (partition by tripkey ORDER BY d.lid) as forsteStoppTid,
		                        parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime) as tid,
		                        rute.LineNameLong
	                        from
		                        (STOPPOINT_DATA d inner join ROUTE_FROM_TO rute on d.RouteFromToKey = rute.RouteFromToKey) 
	                        where
	                            Operating_dateKey in ('" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + @"')
	                        and TripStatus = 1
                        )
                        select count(distinct tripkey) from turer";

            int apcTripCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 300;

                apcTripCount = (int)cmd.ExecuteScalar();

                conn.Close();
            }

            sql = @"with 
                turer as (
	                select
		                d.lid, tripkey, stopkey, Entered_In, Entered_out, d.routefromtokey, stopkey_to,
		                sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as onboard,
		                lead(d.lid) over (partition by tripkey ORDER BY d.lid) as nesteStoppLid,
		                first_value(stopkey) over (partition by tripkey ORDER BY d.lid) as forsteStoppId,
		                first_value(parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime)) over (partition by tripkey ORDER BY d.lid) as forsteStoppTid,
		                parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime) as tid,
		                rute.LineNameLong,
		                kapasitet.kapasitet
	                from
		                (STOPPOINT_DATA d inner join ROUTE_FROM_TO rute on d.RouteFromToKey = rute.RouteFromToKey) left join LinjeKapasitet kapasitet on rute.LineNameLong = kapasitet.LineNameLong
	                where
	                    Operating_dateKey in ('" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + @"')
	                and TripStatus = 1
                ),
                overlast as (
	                select
		                *
	                from 
		                turer
	                where
		                onboard > isnull(kapasitet, 20)

                union all

	                select 
		                neste.*
	                from 
		                turer as denne inner join turer as neste on denne.nesteStoppLid = neste.lid
	                where
		                denne.onboard > isnull(denne.kapasitet, 20)

                )

                select
	                max(LineNameLong) as LineName, max(p.Name) as avgangsstopp, max(forsteStoppTid) as avgangsTid, max(onboard) as ombord, isnull(max(kapasitet), 20) as kapasitet, 
	                cast(max(tid) - min(tid) as time(3)) as overlastTid
                from
	                overlast inner join STOPPOINTS p on overlast.forsteStoppId = p.StopKey
                group by
	                tripkey
                order by 
	                avgangsTid";

            int redCountBus = 0;
            int yellowCountBus = 0;
            int redCountRail = 0;
            int yellowCountRail = 0;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("<table width=100%><tr><th>Linje</th><th>Avgang frå</th><th>Avgangstid</th><th>Passasjerer</th><th>Minutt overlast</th></tr>");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {

                bool altRow = false;

                conn.Open();

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 300;
                using var rows = await cmd.ExecuteReaderAsync();
                while (await rows.ReadAsync())
                {
                    bool isRed = false;

                    if (double.Parse(rows["ombord"].ToString()) > Math.Floor(double.Parse(rows["kapasitet"].ToString()) * 1.5))
                        isRed = true;
                    else if (TimeSpan.Parse(rows["overlastTid"].ToString()).Minutes >= 15)
                        isRed = true;

                    redCountRail += isRed && rows["LineName"].ToString().Equals("1") ? 1 : 0;
                    redCountBus += isRed && !rows["LineName"].ToString().Equals("1") ? 1 : 0;
                    yellowCountRail += !isRed && rows["LineName"].ToString().Equals("1") ? 1 : 0;
                    yellowCountBus += !isRed && !rows["LineName"].ToString().Equals("1") ? 1 : 0;

                    String style = "";
                    if (isRed)
                        style = "color:red;";

                    if (altRow)
                        style += "background-color:lightgray";
                    altRow = !altRow;

                    sb.AppendFormat("<tr style={0}><td>{1}</td><td>{2}</td><td>{3:t}</td><td>{4:#} (+{5:#})</td><td>{6:%m}</td></tr>", style, rows["LineName"], rows["avgangsstopp"], (DateTime)rows["avgangsTid"], (decimal)rows["ombord"], (decimal)rows["ombord"] - (int)rows["kapasitet"], (TimeSpan) rows["overlastTid"]);
                }
                rows.Close();
                conn.Close();
            }

            sb.Append("</table>");

            String json = @"
                {
                    '@type': 'MessageCard',
                    '@context': 'http://schema.org/extensions',
                    'themeColor': '0076D7',
                    'text': 'Passasjertall " + lightRail[0].Key.ToShortDateString() + @" (" + apcTripCount.ToString() + @" turar med APC)',
                    'sections': [
                    {
                        'activityTitle': 'Bybanen - Tal passasjerer: **" + lightRail[0].Value.ToString() + "** " + lightRailChange + @"',
                        'facts': [{                        
                            'name': 'Gule turer',
                            'value': '**" + yellowCountRail.ToString() + @"**'
                        }, {
                            'name': 'Røde turer',
                            'value': '**" + redCountRail.ToString() + @"**'
                        }]
                    },
                    {
                        'activityTitle': 'Buss - Tal passasjerer: **" + bus[0].Value.ToString() + "** " + busChange + @"',
                        'facts': [{                        
                            'name': 'Gule turer',
                            'value': '**" + yellowCountBus.ToString() + @"**'
                        }, {
                            'name': 'Røde turer',
                            'value': '**" + redCountBus.ToString() + @"**'
                        }]
                    },
                    {
                        'activityTitle' : 'Risikoturar',
                        'activityText' : '" + sb.ToString() + @"'
                    }                   
                    ]
                }";

            var response = string.Empty;

            String url = config["TeamsPostUrl"];

            using (var client = new HttpClient())
            {
                HttpResponseMessage result = await client.PostAsync(url, new StringContent(json));
                var responseContent = await result.Content.ReadAsStringAsync();

                log.LogInformation($"Response: {responseContent}");

                if (responseContent.Contains("Microsoft Teams endpoint returned HTTP error 429"))
                {
                    // initiate retry logic
                }
            }
        }
    }
}
