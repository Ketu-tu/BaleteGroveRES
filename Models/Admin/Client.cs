using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models.Admin
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        public int InquiryId { get; set; }
        [ForeignKey("InquiryId")]
        public virtual Inquiry? Inquiry { get; set; }

        public string? AgentUserId { get; set; }
        [ForeignKey("AgentUserId")]
        public virtual ApplicationUser? Agent { get; set; }

        
        public string Status { get; set; } = "Processing"; 

        public DateTime DateAccepted { get; set; }
        public DateTime? DateVisitationScheduled { get; set; }
        public DateTime? DateVisitationFinished { get; set; }
        public DateTime? DatePaid { get; set; }
    }
}
