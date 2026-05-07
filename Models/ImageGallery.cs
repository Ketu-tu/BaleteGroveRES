using System;
using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models.Admin
{
    public class GalleryImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}