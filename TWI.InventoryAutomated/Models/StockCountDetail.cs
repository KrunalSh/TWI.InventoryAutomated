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
        public string WhseDocumentNo { get; set; }
        public string ZoneCode { get; set; }
        public string BinCode { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string LotNo { get; set; }
        public string ExpirationDate { get; set; }
        public string UOMCode { get; set; }
        public Nullable<decimal> PhyicalQty { get; set; }
        public Nullable<decimal> NAVQty { get; set; }
        public string TemplateName { get; set; }
        public string BatchName { get; set; }
        public string LocationCode { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public string FinalSource { get; set; }
        public string CountSource { get; set; }
        public string MethodUsed { get; set; }
    }
}
