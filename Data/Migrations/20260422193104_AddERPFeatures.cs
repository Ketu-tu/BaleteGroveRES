using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaleteGroveRES.Data.Migrations
{
    
    public partial class AddERPFeatures : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyExpenses_AspNetUsers_LoggedByUserId",
                table: "CompanyExpenses");

            migrationBuilder.DropIndex(
                name: "IX_CompanyExpenses_LoggedByUserId",
                table: "CompanyExpenses");

            migrationBuilder.DropColumn(
                name: "LoggedByUserId",
                table: "CompanyExpenses");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CompanyExpenses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CompanyExpenses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CompanyExpenses",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CompanyExpenses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "LoggedByUserId",
                table: "CompanyExpenses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyExpenses_LoggedByUserId",
                table: "CompanyExpenses",
                column: "LoggedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyExpenses_AspNetUsers_LoggedByUserId",
                table: "CompanyExpenses",
                column: "LoggedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
