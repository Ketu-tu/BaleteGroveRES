using System;
using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models.Admin
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        public string? PropertyImage { get; set; }
        public string? ExtraImage1 { get; set; }
        public string? ExtraImage2 { get; set; }
        public string? ExtraImage3 { get; set; }

        public string Type { get; set; }
        public decimal Price { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public int TotalClicks { get; set; } = 0;

        public string Details { get; set; }
        public string PropertyName { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}