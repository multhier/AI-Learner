using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebKoy.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApiKeysHashesAndHits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApiKeyCreatedAt",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "Sites",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Hits",
                table: "Sites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SiteFiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "Sha1",
                table: "SiteFiles",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_ApiKeyHash",
                table: "Sites",
                column: "ApiKeyHash",
                unique: true,
                filter: "[ApiKeyHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sites_ApiKeyHash",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ApiKeyCreatedAt",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Hits",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SiteFiles");

            migrationBuilder.DropColumn(
                name: "Sha1",
                table: "SiteFiles");
        }
    }
}
