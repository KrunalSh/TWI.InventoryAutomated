using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TWI.InventoryAutomated.Models
{
    public class StockCountModel
    {
        public int ID { get; set; }
        public string SCCode { get; set; }

        public string SCDesc { get; set; }

        public string LocationCode { get; set; }

        public decimal TotalItemCount { get; set; }

        public int TotalAdjustmentItems { get; set; }

        public string Status { get; set; }
        
        public DateTime? CreatedDate { get; set; }

        public int CreatedBy { get; set; }

        public List<StockCountDetail> _stockCountItems { get; set; }
    }



    









}