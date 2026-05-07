using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaleteGroveRES.Data.Migrations
{
    
    public partial class AddAgentClientRouting : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquiryId = table.Column<int>(type: "int", nullable: false),
                    AgentUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAccepted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateVisitationScheduled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateVisitationFinished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DatePaid = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_AspNetUsers_AgentUserId",
                        column: x => x.AgentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Clients_Inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalTable: "Inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_AgentUserId",
                table: "Clients",
                column: "AgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_InquiryId",
                table: "Clients",
                column: "InquiryId");
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
