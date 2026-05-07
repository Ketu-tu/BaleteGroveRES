using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaleteGroveRES.Data.Migrations
{
    
    public partial class AddCommissionAndSlip : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                table: "TransactionLedgers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "TransactionLedgers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                table: "TransactionLedgers");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "TransactionLedgers");
        }
    }
}
