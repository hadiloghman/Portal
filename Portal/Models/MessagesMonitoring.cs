//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Portal.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class MessagesMonitoring
    {
        public long Id { get; set; }
        public long ContentId { get; set; }
        public int MessageType { get; set; }
        public int TotalMessages { get; set; }
        public int TotalSuccessfulySended { get; set; }
        public int TotalFailed { get; set; }
        public int TotalWithoutCharge { get; set; }
        public int Status { get; set; }
    }
}
