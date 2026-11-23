using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroNamaa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientKey",
                table: "SalesInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SyncOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ClientKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncOperations_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_SalesInvoices_ClientKey",
                table: "SalesInvoices",
                columns: new[] { "MerchantId", "ClientKey" },
                unique: true,
                filter: "[ClientKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncOperations_ClientKey",
                table: "SyncOperations",
                columns: new[] { "MerchantId", "ClientKey" },
                filter: "[ClientKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_SyncOperations_Operation",
                table: "SyncOperations",
                columns: new[] { "MerchantId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncOperations");

            migrationBuilder.DropIndex(
                name: "UX_SalesInvoices_ClientKey",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ClientKey",
                table: "SalesInvoices");
        }
    }
}
