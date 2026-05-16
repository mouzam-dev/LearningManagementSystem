using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill: inherit the owning teacher's tenancy onto pre-existing
            // courses so OrgAdmin moderation queries pick them up immediately,
            // without requiring teachers to touch each course. SuperAdmin-owned
            // courses (no teacher tenancy) stay null and remain platform-level.
            migrationBuilder.Sql(@"
                UPDATE c
                   SET c.OrganizationId = u.OrganizationId,
                       c.BranchId       = u.BranchId
                  FROM Courses c
                  JOIN Users   u ON u.Id = c.TeacherId
                 WHERE c.OrganizationId IS NULL
                   AND u.OrganizationId IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_BranchId",
                table: "Courses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OrganizationId",
                table: "Courses",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Branches_BranchId",
                table: "Courses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Organizations_OrganizationId",
                table: "Courses",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Branches_BranchId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Organizations_OrganizationId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_BranchId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_OrganizationId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Courses");
        }
    }
}
