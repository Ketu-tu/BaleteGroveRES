using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaleteGroveRES.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePhotoPath { get; set; }

        
        public bool IsOnLeave { get; set; } = false;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlySalesQuota { get; set; } = 10000000m; 

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }

    
    public class AdminProfileViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime DateCreated { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? ProfilePhotoPath { get; set; }

        public IFormFile? PhotoFile { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool IsActive { get; set; }
        public bool IsOnLeave { get; set; }
        public decimal MonthlySalesQuota { get; set; }
    }
}