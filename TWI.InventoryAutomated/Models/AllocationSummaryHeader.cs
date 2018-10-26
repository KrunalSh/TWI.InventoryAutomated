using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Models
{
    public class AllocationSummaryHeader
    {
        public string BatchCode { get; set; }
        public string CountName { get; set; }
        public int TotalLines { get; set; }
        public int NAVLines { get; set; }
        public int NewLines { get; set; }
        public int RemainingTotal { get; set; }
        public int RemainingNAV { get; set; }
        public int RemainingNew { get; set; }
    }
}