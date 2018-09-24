﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Models
{
    public class BatchIterationModel
    {
        public int ID { get; set; }
        public string SCCode { get; set; }

        public string SCDesc { get; set; }

        public string LocationCode { get; set; }

        public decimal TotalItemCount { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int CreatedBy { get; set; }

        public int CountAllocationTotal { get; set; }

        public List<StockCountIterations> Iterations { get; set; }

        public List<StockCountTeams> Teams { get; set; }

        public List<CountItemsSummary> TeamAllocationSummary { get; set; }
    }



    public class CountItemsSummary {
        public int CountID { get; set; }

        public int TeamId { get; set; }

        public int TotalItems { get; set; }
    }


}