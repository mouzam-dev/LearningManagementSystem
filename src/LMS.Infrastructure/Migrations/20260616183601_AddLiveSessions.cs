using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LiveSessionId",
                table: "AttendanceSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LiveSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HostTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Users_HostTeacherId",
                        column: x => x.HostTeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_LiveSessionId",
                table: "AttendanceSessions",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_BranchId_ScheduledStart",
                table: "LiveSessions",
                columns: new[] { "BranchId", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_CourseId_ScheduledStart",
                table: "LiveSessions",
                columns: new[] { "CourseId", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_HostTeacherId",
                table: "LiveSessions",
                column: "HostTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_OrganizationId",
                table: "LiveSessions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_RoomName",
                table: "LiveSessions",
                column: "RoomName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_LiveSessionId",
                table: "AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "LiveSessionId",
                table: "AttendanceSessions");
        }
    }
}
