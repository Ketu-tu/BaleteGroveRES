using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models.Admin
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Module { get; set; }

        [Required]
        public string Action { get; set; }

        public string Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
