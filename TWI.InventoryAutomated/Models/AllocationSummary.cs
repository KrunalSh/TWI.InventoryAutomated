using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Models
{
    public class AllocationSummary
    {
        public string TeamName { get; set; }
        public int NavLines { get; set; }
        public int NewLines { get; set; }
        public int TotalLines { get; set; }
        public decimal Percent { get; set; }
    }
}