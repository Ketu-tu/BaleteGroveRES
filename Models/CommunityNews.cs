using System;
using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models.Admin
{
    public class CommunityNews
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        public string Information { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}