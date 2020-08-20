using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace TrafficLoadUpdater
{
    public static class UpdateTeams
    {
        [FunctionName("SendTrafficLoadUpdate")]
        public static async void Run([TimerTrigger("0 15 8 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
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
		                d.lid, tripkey, stopkey, Entered_In, Entered_out, d.routefromtokey, stopkey_to, tripstatus,
		                sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as onboard,
		                (case when sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) > isnull(kapasitet * 0.5, 20) and TripStatus = 1 then 1 else 0 end) as overlastR,
		                (case when sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) > isnull(kapasitet * 0.75, 30) and TripStatus = 1 then 1 else 0 end) as overlastY,
		                (case when sum(entered_in-entered_out) over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) > isnull(kapasitet, 40) and TripStatus = 1 then 1 else 0 end) as overlastG,
		                lead(d.lid) over (partition by tripkey ORDER BY d.lid) as nesteStoppLid,
		                first_value(stopkey) over (partition by tripkey ORDER BY d.lid) as forsteStoppId,
		                rute.from_To as ruteNamn,
		                --string_agg(d.StopKey, ',') over (partition by tripkey ORDER BY d.lid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as rute,
		                first_value(parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime)) over (partition by tripkey ORDER BY d.lid) as forsteStoppTid,
		                parse(concat(substring(Actual_Datekey, 1, 4), '.', substring(Actual_Datekey, 5, 2), '.', substring(Actual_Datekey, 7, 2), ' ', substring(TimeKey, 1, 2), ':', substring(TimeKey, 3, 2)) as datetime) as tid,
		                rute.LineNameLong,
		                kapasitet.kapasitet		
	                from
		                (STOPPOINT_DATA d inner join ROUTE_FROM_TO rute on d.RouteFromToKey = rute.RouteFromToKey) left join LinjeKapasitet kapasitet on rute.LineNameLong = kapasitet.LineNameLong
	                where
	                    Operating_dateKey in ('" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + @"')
                )

                insert into overlastturer
                select
	                distinct 
	                tripkey,
	                max(tripstatus) over (partition by TripKey) as tripStatus,
	                LineNameLong as LineName,
	                p.Name as avgangsstopp,
	                forsteStoppTid as avgangstid,
                	round(sum(entered_in) over (partition by TripKey), 0) as paastigende,
	                max(onboard) over (partition by TripKey) as ombord,
	                isnull(kapasitet, 40) as kapasitet,
	                first_value(p2.Name) over (partition by tripkey ORDER BY overlastR desc, tid ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as fraStoppR,
	                first_value(p2.Name) over (partition by tripkey ORDER BY overlastY desc, tid ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as fraStoppY,
	                first_value(p2.Name) over (partition by tripkey ORDER BY overlastG desc, tid ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as fraStoppG,
	                min(case when overlastR = 1 then tid end) over (partition by TripKey order by tid RANGE BETWEEN unbounded preceding and UNBOUNDED FOLLOWING) as fraTidR,
	                max(case when overlastR = 1 then tid end) over (partition by TripKey ORDER BY tid RANGE BETWEEN unbounded preceding AND UNBOUNDED FOLLOWING) as tilTidR,
	                min(case when overlastY = 1 then tid end) over (partition by TripKey order by tid RANGE BETWEEN unbounded preceding and UNBOUNDED FOLLOWING) as fraTidY,
	                max(case when overlastY = 1 then tid end) over (partition by TripKey ORDER BY tid RANGE BETWEEN unbounded preceding AND UNBOUNDED FOLLOWING) as tilTidY,
	                min(case when overlastG = 1 then tid end) over (partition by TripKey order by tid RANGE BETWEEN unbounded preceding and UNBOUNDED FOLLOWING) as fraTidG,
	                max(case when overlastG = 1 then tid end) over (partition by TripKey ORDER BY tid RANGE BETWEEN unbounded preceding AND UNBOUNDED FOLLOWING) as tilTidG,
	                max(rutenamn) over (partition by TripKey) as rutenamn,
	                max(RouteFromToKey) over (partition by TripKey) as RouteFromToKey
	
                from
	                turer inner join STOPPOINTS p on turer.forsteStoppId = p.StopKey inner join STOPPOINTS p2 on turer.StopKey = p2.StopKey";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using SqlCommand sniffer = new SqlCommand("select count(tripkey) from OverlastTurer where convert(date, avgangstid) = '" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "'", conn);
                var snifferResult = (int)sniffer.ExecuteScalar();

                if (snifferResult < 500) { 
                    using SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.CommandTimeout = 300;
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }

            sql = @"select
	                    case when LineName = '1' then 'Bybanen' else 'Buss' end as Line,
	                    sum(case 
		                    when tripstatus = 1 and (ombord >= floor(kapasitet * 0.75) * 1.25 or datediff(minute, fratidY, tilTidY) >= 15) then 1 
		                    else 0
		                    end) as raude,
	                    sum(case 
		                    when tripstatus = 1 and (ombord between floor(kapasitet * 0.75) and floor((kapasitet * 0.75) * 1.25) and datediff(minute, fratidY, tilTidY) < 15) then 1 
		                    else 0
		                    end) as gule,
	                    sum(case 
		                    when tripstatus = 1 and ombord <= kapasitet * 0.75 then 1
		                    else 0
		                    end) as grone,
	                    sum(case when tripstatus != 1 then 1 else 0 end) as ukjente
                    from 
	                    OverlastTurer 
                    where 
	                    convert(date, avgangstid) = '" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + @"'
                    group by
	                    case when LineName = '1' then 'Bybanen' else 'Buss' end";

            int redCountBus = 0;
            int yellowCountBus = 0;
            int redCountRail = 0;
            int yellowCountRail = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 300;

                using var rows = await cmd.ExecuteReaderAsync();
                while (await rows.ReadAsync())
                {
                    if (rows["Line"].ToString().Equals("Buss"))
                    {
                        redCountBus = Convert.ToInt32(rows["raude"].ToString());
                        yellowCountBus = Convert.ToInt32(rows["gule"].ToString());
                    } else
                    {
                        redCountRail = Convert.ToInt32(rows["raude"].ToString());
                        yellowCountRail = Convert.ToInt32(rows["gule"].ToString());
                    }
                }
                rows.Close();
                conn.Close();
            }

            String url = "https://trafficload.azurewebsites.net/Yellow/Yellow/" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            String json = @"
                {
                    '@type': 'MessageCard',
                    '@context': 'http://schema.org/extensions',
                    'themeColor': '0076D7',
                    'text': 'Passasjertall " + lightRail[0].Key.ToShortDateString() + @" (" + apcTripCount.ToString() + @" turar med APC) - <a href=""" + url + @""">Til nettsida</a>',
                    'sections': [
                    {
                        'activityTitle': 'Bybanen - Tal passasjerer: **" + lightRail[0].Value.ToString() + "** " + lightRailChange + @"',
                        'facts': [{                        
                            'name': 'Gule turer',
                            'value': '<b><a href=""" + url + @""">" + yellowCountRail.ToString() + @"</a></b>'
                        }, {
                            'name': 'Røde turer',
                            'value': '<b><a href=""" + url + @""">" + redCountRail.ToString() + @"</a></b>'
                        }]
                    },
                    {
                        'activityTitle': 'Buss - Tal passasjerer: **" + bus[0].Value.ToString() + "** " + busChange + @"',
                        'facts': [{                        
                            'name': 'Gule turer',
                            'value': '<b><a href=""" + url + @""">" + yellowCountBus.ToString() + @"</a></b>'
                        }, {
                            'name': 'Røde turer',
                            'value': '<b><a href=""" + url + @""">" + redCountBus.ToString() + @"</a></b>'
                        }]
                    }                   
                    ]
                }";

            var response = string.Empty;

            String teamsUrl = config["TeamsPostUrl"];

            using (var client = new HttpClient())
            {
                HttpResponseMessage result = await client.PostAsync(teamsUrl, new StringContent(json));
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
