using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kivilcim.Dal.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostImagesGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsExternal = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostImages_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostImages_PostId_SortOrder",
                table: "PostImages",
                columns: new[] { "PostId", "SortOrder" });

            // Eski tek görselli gönderilerin resmini yeni tabloya taşı, sonra eski sütunu kaldır.
            migrationBuilder.Sql(@"INSERT INTO [PostImages] ([PostId], [Url], [IsExternal], [SortOrder])
SELECT [Id], [ImagePath], 0, 0 FROM [Posts] WHERE [ImagePath] IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Posts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql(@"UPDATE p SET p.[ImagePath] = i.[Url]
FROM [Posts] p JOIN [PostImages] i ON i.[PostId] = p.[Id] AND i.[SortOrder] = 0 AND i.[IsExternal] = 0;");

            migrationBuilder.DropTable(
                name: "PostImages");

        }
    }
}
