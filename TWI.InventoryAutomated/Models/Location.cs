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
    
    public partial class Location
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public Nullable<int> InstanceID { get; set; }
        public Nullable<int> CompanyID { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
    }
}
