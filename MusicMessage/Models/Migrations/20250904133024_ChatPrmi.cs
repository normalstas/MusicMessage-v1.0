using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class ChatPrmi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.CreateTable(
		name: "ChatPreviews",
		columns: table => new
		{
			ChatPreviewId = table.Column<int>(type: "int", nullable: false)
				.Annotation("SqlServer:Identity", "1, 1"),
			UserId = table.Column<int>(type: "int", nullable: false),
			OtherUserId = table.Column<int>(type: "int", nullable: false),
			OtherUserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
			LastMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
			LastMessageTime = table.Column<DateTime>(type: "datetime2", nullable: false),
			UnreadCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
		},
		constraints: table =>
		{
			table.PrimaryKey("PK_ChatPreviews", x => x.ChatPreviewId);
			table.ForeignKey(
				name: "FK_ChatPreviews_User_OtherUserId",
				column: x => x.OtherUserId,
				principalTable: "User",
				principalColumn: "UserId",
				onDelete: ReferentialAction.NoAction);
			table.ForeignKey(
				name: "FK_ChatPreviews_User_UserId",
				column: x => x.UserId,
				principalTable: "User",
				principalColumn: "UserId",
				onDelete: ReferentialAction.NoAction); 
		});

			migrationBuilder.CreateIndex(
				name: "IX_ChatPreviews_OtherUserId",
				table: "ChatPreviews",
				column: "OtherUserId");

			migrationBuilder.CreateIndex(
				name: "IX_ChatPreviews_UserId",
				table: "ChatPreviews",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_ChatPreviews_UserId_OtherUserId",
				table: "ChatPreviews",
				columns: new[] { "UserId", "OtherUserId" },
				unique: true);
			
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatPreviews");
        }
    }
}
