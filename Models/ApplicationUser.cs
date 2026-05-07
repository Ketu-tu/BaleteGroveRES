using Microsoft.AspNetCore.Identity;
using System;

namespace BaleteGroveRES.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
