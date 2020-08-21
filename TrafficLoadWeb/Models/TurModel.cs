using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLoadWeb.Models
{

    public enum TrafficLightStatus
    {
        Red = 50,
        Yellow = 75,
        Green = 100
    }
    public abstract class Tur
    {
        public String? FraStopp(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (CurrentStatus == TrafficLightStatus.Red)
                return this.FraStoppR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return this.FraStoppY;
            else
                return this.FraStoppG;
        }

        public DateTime? FraTid(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (CurrentStatus == TrafficLightStatus.Red)
                return this.FraTidR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return this.FraTidY;
            else
                return this.FraTidG;
        }

        public DateTime? TilTid(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (CurrentStatus == TrafficLightStatus.Red)
                return this.TilTidR;
            else if (CurrentStatus == TrafficLightStatus.Yellow)
                return this.TilTidY;
            else
                return this.TilTidG;
        }

        public int StatusKapasitet(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            return (int) Math.Floor(((double)this.Kapasitet * ((int)CurrentStatus) / 100.0));
        }

        public bool IsRed(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (!this.FraTid(CurrentStatus).HasValue || !this.TilTid(CurrentStatus).HasValue || this.IsUnknown(CurrentStatus))
                return false;

            var factor = ((int) CurrentStatus) / 100.0;

            if ((double)this.Ombord > (this.Kapasitet * (factor * 1.25)))
                return true;
            else if (this.TilTid(CurrentStatus).Value.Subtract(this.FraTid(CurrentStatus).Value).TotalMinutes >= 15.0)
                return true;

            return false;
        }

        public bool IsYellow(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (this.IsRed(CurrentStatus) || this.IsUnknown(CurrentStatus))
                return false;

            var factor = ((int)CurrentStatus) / 100.0;

            return (double)this.Ombord > (this.Kapasitet * factor);
        }

        public bool IsGreen(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            if (this.IsYellow(CurrentStatus) || this.IsUnknown(CurrentStatus))
                return false;

            var factor = ((int)CurrentStatus) / 100.0;

            return (double)this.Ombord <= (this.Kapasitet * factor);
        }

        public bool IsUnknown(TrafficLightStatus CurrentStatus = TrafficLightStatus.Red)
        {
            return this.TripStatus != 1;
        }

        [Key]
        public String TripKey { get; set; } = "";

        public int TripStatus { get; set; }
        public String LineName { get; set; } = "";
        public String AvgangsStopp { get; set; } = "";
        public String TilStopp { 
            get {
                return RuteNamn.Remove(0, RuteNamn.IndexOf("-") + 1);
            } 
        }
        public DateTime AvgangsTid { get; set; }
        public decimal Paastigende { get; set; }
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

    }

    [Table("OverlastTurer")]
    public class TurModel : Tur
    {

    }

    [Table("OverlastTurerHistorie")]
    public class TurModelHistory : Tur
    {
        public String MasterTripKey { get; set; }
        public DateTime MasterTid { get; set; }
    }
}
