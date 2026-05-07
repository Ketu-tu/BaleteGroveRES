using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models.Admin
{
    public class TransactionLedger
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        public string AgentUserId { get; set; }
        [ForeignKey("AgentUserId")]
        public virtual ApplicationUser Agent { get; set; }

        public string BuyerName { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public decimal SaleAmount { get; set; }
        
        public decimal CommissionAmount { get; set; }
        public string ReferenceNumber { get; set; }

        public bool IsCommissionPaid { get; set; } = false;
        public DateTime? DateCommissionPaid { get; set; }
    }
}
