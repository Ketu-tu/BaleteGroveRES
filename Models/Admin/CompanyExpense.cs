using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models.Admin
{
    public class CompanyExpense
    {
        [Key]
        public int Id { get; set; }

        public DateTime DateIncurred { get; set; } = DateTime.Now;

        [Required]
        public string Category { get; set; }

        [Required]
        public string Description { get; set; }

        public decimal Amount { get; set; }
    }
}
