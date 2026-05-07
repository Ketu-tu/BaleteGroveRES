using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models.Admin
{
    public class Inquiry
    {
        [Key]
        public int Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Reason { get; set; }

        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public int PropertyId { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        public string? AgentUserId { get; set; }
        
        [ForeignKey("AgentUserId")]
        public virtual ApplicationUser? Agent { get; set; }

        
        public string Status { get; set; } = "Pending";
    }
}
