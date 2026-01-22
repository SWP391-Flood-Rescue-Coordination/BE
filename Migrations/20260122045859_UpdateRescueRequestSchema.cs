using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flood_Rescue_Coordination.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRescueRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rescue_requests",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    citizen_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    priority_level_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    number_of_people = table.Column<int>(type: "int", nullable: false),
                    has_children = table.Column<bool>(type: "bit", nullable: false),
                    has_elderly = table.Column<bool>(type: "bit", nullable: false),
                    has_disabled = table.Column<bool>(type: "bit", nullable: false),
                    special_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rescue_requests", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_rescue_requests_users_citizen_id",
                        column: x => x.citizen_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_rescue_requests_citizen_id",
                table: "rescue_requests",
                column: "citizen_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rescue_requests");
        }
    }
}
