using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaleteGroveRES.Data.Migrations
{
    
    public partial class AddPropertyArchiveFlag : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnLeave",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalesQuota",
                table: "UserProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Properties",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnLeave",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MonthlySalesQuota",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Properties");
        }
    }
}
