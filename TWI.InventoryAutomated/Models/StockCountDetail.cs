//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TWI.InventoryAutomated.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class StockCountDetail
    {
        public int ID { get; set; }
        public Nullable<int> SCID { get; set; }
        public string Whse__Document_No_ { get; set; }
        public string Zone_Code { get; set; }
        public string Bin_Code { get; set; }
        public string Item_No_ { get; set; }
        public string Description { get; set; }
        public string Lot_No_ { get; set; }
        public Nullable<System.DateTime> Expiration_Date { get; set; }
        public string Unit_of_Measure_Code { get; set; }
        public Nullable<decimal> PhyicalQty { get; set; }
        public Nullable<decimal> NAVQty { get; set; }
        public string Template_Name { get; set; }
        public string Batch_Name { get; set; }
        public string Location_Code { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> CreatedBy { get; set; }
    }
}
