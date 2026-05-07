using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models.Admin
{
    public class PropertyStatus
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        
        public string Status { get; set; } = "Available";
    }
}
