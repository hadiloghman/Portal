//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JhoobinMedadLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Otp
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string Type { get; set; }
        public string ReturnValue { get; set; }
        public string UserMessage { get; set; }
    }
}