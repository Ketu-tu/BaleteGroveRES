using System.ComponentModel.DataAnnotations;

namespace BaleteGroveRES.Models
{
    public class UserManagementViewModel
    {
        public List<UserDisplayInfo> Users { get; set; } = new();
    }

    public class UserDisplayInfo
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public string? ProfilePhotoPath { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        public string FullName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
    }
}