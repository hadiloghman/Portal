//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NebulaLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SinglechargeArchive
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public string ReferenceId { get; set; }
        public System.DateTime DateCreated { get; set; }
        public string PersianDateCreated { get; set; }
        public int Price { get; set; }
        public bool IsSucceeded { get; set; }
        public string Description { get; set; }
        public bool IsApplicationInformed { get; set; }
        public Nullable<long> InstallmentId { get; set; }
        public bool IsCalledFromInAppPurchase { get; set; }
    }
}
