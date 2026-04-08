using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerOrder.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddGdprAndAuditingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "customerorder",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                schema: "customerorder",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "customerorder",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "customerorder",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Changes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "CustomerConsents",
                schema: "customerorder",
                columns: table => new
                {
                    ConsentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConsentVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerConsents", x => x.ConsentId);
                    table.ForeignKey(
                        name: "FK_CustomerConsents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "customerorder",
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                schema: "customerorder",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionDate",
                schema: "customerorder",
                table: "AuditLogs",
                column: "ActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                schema: "customerorder",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserEmail",
                schema: "customerorder",
                table: "AuditLogs",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConsents_CustomerId",
                schema: "customerorder",
                table: "CustomerConsents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConsents_CustomerId_ConsentType",
                schema: "customerorder",
                table: "CustomerConsents",
                columns: new[] { "CustomerId", "ConsentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "customerorder");

            migrationBuilder.DropTable(
                name: "CustomerConsents",
                schema: "customerorder");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsDeleted",
                schema: "customerorder",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "customerorder",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                schema: "customerorder",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "customerorder",
                table: "Customers");
        }
    }
}
