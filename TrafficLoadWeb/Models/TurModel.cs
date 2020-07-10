using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrafficLoadWeb.Models;

namespace TrafficLoadWeb.Models
{

    public enum TrafficLightStatus
    {
        Red = 50,
        Yellow = 75,
        Green = 100
    }

    public interface ITurModel
    {
        public String TripKey { get; set; }
        public int TripStatus { get; set; }
        public String LineName { get; set; }
        public String AvgangsStopp { get; set; }
        public DateTime AvgangsTid { get; set; }
        public decimal Ombord { get; set; }
        public int Kapasitet { get; set; }
        public String RouteFromToKey { get; set; }
        public String RuteNamn { get; set; }

#nullable enable
        public String? FraStoppR { get; set; }
        public DateTime? FraTidR { get; set; }
        public DateTime? TilTidR { get; set; }
        public String? FraStoppY { get; set; }
        public DateTime? FraTidY { get; set; }
        public DateTime? TilTidY { get; set; }
        public String? FraStoppG { get; set; }
        public DateTime? FraTidG { get; set; }
        public DateTime? TilTidG { get; set; }
        public String? FraStopp { get; }
        public DateTime? FraTid { get; }
        public DateTime? TilTid { get; }

        public bool isRed { get; }
        public bool isYellow{ get; }
        public bool isGreen { get; }
        public bool isUnknown { get; }

    }

    public interface ITurModelHelper
    {
        public TrafficLightStatus CurrentStatus { get; }
        public String? FraStopp(ITurModel m);
        public DateTime? FraTid(ITurModel m);
        public DateTime? TilTid(ITurModel m);
        public bool IsRed(ITurModel m);
        public bool IsYellow(ITurModel m);
        public bool IsGreen(ITurModel m);
        public bool IsUnknown(ITurModel m);

    }

    public class TurModelHelper : ITurModelHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TurModelHelper(IHttpContextAccessor acc)
        {
            _httpContextAccessor = acc;

            String status = "Red";
            if (acc.HttpContext.Request.RouteValues.ContainsKey("status"))
                status = acc.HttpContext.Request.RouteValues["status"].ToString();

            Factor = ((int) Enum.Parse(typeof(TrafficLightStatus), status, true)) / 100.0;
        }

        public TurModelHelper()
        {
            Factor = .50;
        }

        public TrafficLightStatus CurrentStatus 
        { 
            get {
                return (TrafficLightStatus)Enum.ToObject(typeof(TrafficLightStatus), (int)Factor * 100);
            } 
        }

        internal double Factor { get; set; } = .5;

        public String? FraStopp(ITurModel m) {
            if (CurrentStatus == TrafficLightStatus.Red)
                return m.FraStoppR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return m.FraStoppY;
            else
                return m.FraStoppG;
        }
        public DateTime? FraTid(ITurModel m)
        {
            if (CurrentStatus == TrafficLightStatus.Red)
                return m.FraTidR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return m.FraTidY;
            else
                return m.FraTidG;
        }

        public DateTime? TilTid(ITurModel m)
        {
            if (CurrentStatus == TrafficLightStatus.Red)
                return m.TilTidR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return m.TilTidY;
            else
                return m.TilTidG;
        }

        public bool IsRed(ITurModel m)
        {
            if (!m.FraTid.HasValue || !m.TilTid.HasValue)
                return false;

            if ((double)m.Ombord > (m.Kapasitet * (Factor * 1.25)))
                return true;
            else if (m.TilTid.Value.Subtract(m.FraTid.Value).TotalMinutes >= 15.0)
                return true;

            return false;
        }

        public bool IsYellow(ITurModel m)
        {
            if (this.IsRed(m))
                return false;

            return (double)m.Ombord > (m.Kapasitet * Factor);         
        }

        public bool IsGreen(ITurModel m)
        {
            if (this.IsYellow(m))
                return false;

            return (double)m.Ombord <= (m.Kapasitet * Factor);
        }

        public bool IsUnknown(ITurModel m)
        {
            return m.TripStatus != 1;
        }

    }

    [Table("OverlastTurer")]
    public class TurModel : ITurModel
    {
        private ITurModelHelper? _helper = new TurModelHelper();

        private TurModel(Data.TrafficLoadContext ctx)
        {
            var s = ctx.Turer;
        }

        public ITurModelHelper Helper
        {
            set
            {
                _helper = value;
            }
        }

        [Key]
        public String TripKey { get; set; } = "";
        public int TripStatus { get; set; }
        public String LineName { get; set; } = "";
        public String AvgangsStopp { get; set; } = "";
        public DateTime AvgangsTid { get; set; }
        public decimal Ombord { get; set; }
        public int Kapasitet { get; set; }
        public String RouteFromToKey { get; set; } = "";
        public String RuteNamn { get; set; } = "";

#nullable enable
        public String? FraStoppR { get; set; }
        public DateTime? FraTidR { get; set; }
        public DateTime? TilTidR { get; set; }
        public String? FraStoppY { get; set; }
        public DateTime? FraTidY { get; set; }
        public DateTime? TilTidY { get; set; }
        public String? FraStoppG { get; set; }
        public DateTime? FraTidG { get; set; }
        public DateTime? TilTidG { get; set; }
#nullable disable
        [NotMapped, ForeignKey("MasterTripKey")]
        public virtual ICollection<TurModelHistory> History { get; set; }

        public String? FraStopp {  get { return _helper.FraStopp(this); } }
        public DateTime? FraTid { get { return _helper.FraTid(this); } }
        public DateTime? TilTid { get { return _helper.TilTid(this); } }

        public bool isRed
        {
            get
            {
                return _helper.IsRed(this);
            }
        }

        public bool isYellow { 
            get {
                return _helper.IsYellow(this);
            }
        }

        public bool isGreen
        {
            get
            {
                return _helper.IsGreen(this);
            }
        }

        public bool isUnknown
        {
            get
            {
                return _helper.IsUnknown(this);
            }
        }
    }

    [Table("OverlastTurerHistorie")]
    public class TurModelHistory : ITurModel
    {
        private ITurModelHelper? _helper = new TurModelHelper();

        public ITurModelHelper Helper
        {
            set
            {
                _helper = value;
            }
        }

        public String MasterTripKey { get; set; }
        public DateTime MasterTid { get; set; }
        [Column("cTripKey")]
        [Key]
        public String TripKey { get;set;}
        [Column("cTripStatus")]
        public int TripStatus { get;set;}
        [Column("cLineName")]
        public String LineName { get;set;}
        [Column("cAvgangsStopp")]
        public String AvgangsStopp { get;set;}
        [Column("cAvgangsTid")]
        public DateTime AvgangsTid { get;set;}
        [Column("cOmbord")]
        public decimal Ombord { get;set;}
        [Column("cKapasitet")]
        public int Kapasitet { get;set;}
        [Column("cRouteFromToKey")]
        public String RouteFromToKey { get;set;}
        [Column("cRuteNamn")]
        public String RuteNamn { get;set;}
        [Column("cFraStoppR")]
        public String FraStoppR { get;set;}
        [Column("cFraTidR")]
        public DateTime? FraTidR { get;set;}
        [Column("cTilTidR")]
        public DateTime? TilTidR { get;set;}

        [Column("cFraStoppY")]
        public String FraStoppY { get; set; }
        [Column("cFraTidY")]
        public DateTime? FraTidY { get; set; }
        [Column("cTilTidY")]
        public DateTime? TilTidY { get; set; }

        [Column("cFraStoppG")]
        public String FraStoppG { get; set; }
        [Column("cFraTidG")]
        public DateTime? FraTidG { get; set; }
        [Column("cTilTidG")]
        public DateTime? TilTidG { get; set; }

        public String? FraStopp { get { return _helper.FraStopp(this); } }
        public DateTime? FraTid { get { return _helper.FraTid(this); } }
        public DateTime? TilTid { get { return _helper.TilTid(this); } }


        public bool isRed
        {
            get
            {
                return _helper.IsRed(this);
            }
        }

        public bool isYellow
        {
            get
            {
                return _helper.IsYellow(this);
            }
        }

        public bool isGreen
        {
            get
            {
                return _helper.IsGreen(this);
            }
        }

        public bool isUnknown
        {
            get
            {
                return _helper.IsUnknown(this);
            }
        }
    }
}
