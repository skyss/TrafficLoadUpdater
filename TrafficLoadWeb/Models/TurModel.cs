using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrafficLoadWeb.Models;

namespace TrafficLoadWeb.Models
{

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
        public String? FraStopp { get; set; }
        public DateTime? FraTid { get; set; }
        public DateTime? TilTid { get; set; }

        public bool isRed { get; }
        public bool isYellow{ get; }
        public bool isGreen { get; }
        public bool isUnknown { get; }

    }

    internal class TurModelHelper
    {

        internal static bool IsRed(ITurModel m)
        {
            if (!m.FraTid.HasValue || !m.TilTid.HasValue)
                return false;

            if ((double)m.Ombord > m.Kapasitet * 1.5)
                return true;
            else if (m.TilTid.Value.Subtract(m.FraTid.Value).TotalMinutes >= 15.0)
                return true;

            return false;
        }

        internal static bool IsYellow(ITurModel m)
        {
            if (TurModelHelper.IsRed(m))
                return false;

            return m.Ombord > m.Kapasitet;         
        }

        internal static bool IsGreen(ITurModel m)
        {
            if (TurModelHelper.IsYellow(m))
                return false;

            return m.Ombord <= m.Kapasitet;
        }

        internal static bool IsUnknown(ITurModel m)
        {
            return m.TripStatus != 1;
        }

    }

    [Table("OverlastTurer")]
    public class TurModel : ITurModel
    {
        [Key]
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
        public String? FraStopp { get; set; }
        public DateTime? FraTid { get; set; }
        public DateTime? TilTid { get; set; }
#nullable disable
        [NotMapped, ForeignKey("MasterTripKey")]
        public virtual ICollection<TurModelHistory> History { get; set; }

        public bool isRed
        {
            get
            {
                return TurModelHelper.IsRed(this);
            }
        }

        public bool isYellow { 
            get {
                return TurModelHelper.IsYellow(this);
            }
        }

        public bool isGreen
        {
            get
            {
                return TurModelHelper.IsGreen(this);
            }
        }

        public bool isUnknown
        {
            get
            {
                return TurModelHelper.IsUnknown(this);
            }
        }
    }

    [Table("OverlastTurerHistorie")]
    public class TurModelHistory : ITurModel
    {
        public String MasterTripKey { get; set; }
        public DateTime MasterTid { get; set; }
        [Column("cTripKey")]
        [Key]
        public string TripKey { get;set;}
        [Column("cTripStatus")]
        public int TripStatus { get;set;}
        [Column("cLineName")]
        public string LineName { get;set;}
        [Column("cAvgangsStopp")]
        public string AvgangsStopp { get;set;}
        [Column("cAvgangsTid")]
        public DateTime AvgangsTid { get;set;}
        [Column("cOmbord")]
        public decimal Ombord { get;set;}
        [Column("cKapasitet")]
        public int Kapasitet { get;set;}
        [Column("cRouteFromToKey")]
        public string RouteFromToKey { get;set;}
        [Column("cRuteNamn")]
        public string RuteNamn { get;set;}
        [Column("cFraStopp")]
        public string FraStopp { get;set;}
        [Column("cFraTid")]
        public DateTime? FraTid { get;set;}
        [Column("cTilTid")]
        public DateTime? TilTid { get;set;}

        public bool isRed
        {
            get
            {
                return TurModelHelper.IsRed(this);
            }
        }

        public bool isYellow
        {
            get
            {
                return TurModelHelper.IsYellow(this);
            }
        }

        public bool isGreen
        {
            get
            {
                return TurModelHelper.IsGreen(this);
            }
        }

        public bool isUnknown
        {
            get
            {
                return TurModelHelper.IsUnknown(this);
            }
        }
    }
}
