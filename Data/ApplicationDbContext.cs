using BaleteGroveRES.Models.Admin;
using BaleteGroveRES.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BaleteGroveRES.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        
        public DbSet<CommunityNews> CommunityNews { get; set; }

        public DbSet<GalleryImage> GalleryImages { get; set; }

        public DbSet<SiteSettingFAQ> SiteSettingFAQs { get; set; }

        public DbSet<Property> Properties { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Inquiry> Inquiries { get; set; }
        public DbSet<PropertyStatus> PropertyStatuses { get; set; }
        public DbSet<TransactionLedger> TransactionLedgers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<CompanyExpense> CompanyExpenses { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            
            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();
        

        
        builder.Entity<CommunityNews>().ToTable("CommunityNews");

            builder.Entity<GalleryImage>().ToTable("GalleryImages");

            builder.Entity<SiteSettingFAQ>().ToTable("SiteSettingFAQs");

            builder.Entity<Property>().ToTable("Properties")
                .Property(p => p.Price)
                .HasPrecision(18, 2); ;

            builder.Entity<Inquiry>().ToTable("Inquiries");
            builder.Entity<PropertyStatus>().ToTable("PropertyStatuses");
            builder.Entity<TransactionLedger>().ToTable("TransactionLedgers");
            builder.Entity<SystemLog>().ToTable("SystemLogs");
            builder.Entity<CompanyExpense>().ToTable("CompanyExpenses");
        }
    }
}