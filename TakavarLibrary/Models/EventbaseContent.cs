//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TakavarLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class EventbaseContent
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public System.DateTime DateCreated { get; set; }
        public string PersianDateCreated { get; set; }
        public int Price { get; set; }
        public int Point { get; set; }
        public int SubscriberNotSendedMoInDays { get; set; }
        public bool IsAddingMessagesToSendQueue { get; set; }
        public bool IsAddedToSendQueueFinished { get; set; }
    }
}
