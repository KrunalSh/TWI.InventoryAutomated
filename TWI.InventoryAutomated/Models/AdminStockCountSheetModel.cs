using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Models
{
    public class AdminStockCountSheetModel
    {
        public int ID { get; set; }
        public string SCCode { get; set; }

        public string SCDesc { get; set; }

        public string LocationCode { get; set; }

        public decimal TotalItemCount { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int CreatedBy { get; set; }

        public string CountName { get; set; } 

        public string TeamCode { get; set; }

        public string CountInfo { get; set; }

        public List<StockCountAllocations> AllocatedItems { get; set; }

    }
}