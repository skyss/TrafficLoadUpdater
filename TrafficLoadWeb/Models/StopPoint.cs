using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLoadWeb.Models
{
    [Table("ActiveStopPoints")]
    public class StopPoint
    {
        [Key]
        public long StopKey { get; set; }

        [Column("DisplayName")]
        public String Name { get; set; }

        public String AltStopKeys { get; set; }
    }
}
