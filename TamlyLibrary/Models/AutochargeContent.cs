//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TamlyLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class AutochargeContent
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public System.DateTime DateCreated { get; set; }
        public int Point { get; set; }
        public int Price { get; set; }
        public bool IsAddedToSendQueue { get; set; }
        public string PersianDateCreated { get; set; }
        public string Title { get; set; }
        public Nullable<System.DateTime> SendDate { get; set; }
        public string PersianSendDate { get; set; }
    }
}
