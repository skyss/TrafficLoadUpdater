using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLoadWeb.Models
{
    [Table("route_from_to")]
    public class ServiceLine
    {
        [Key]
        public long LID { get; set; }

        public String RouteFromToKey { get; set; }

        public String LineNameLong { get; set; }

        [Column("From_To")]
        public String DisplayName { get; set; }
    }
}
