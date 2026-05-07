using System;
using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models.Admin
{
    public class SiteSettingFAQ
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}