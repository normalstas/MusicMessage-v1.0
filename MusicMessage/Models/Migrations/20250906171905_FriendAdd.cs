using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class FriendAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatPreviews_User_OtherUserId",
                table: "ChatPreviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatPreviews_User_UserId",
                table: "ChatPreviews");

            migrationBuilder.DropIndex(
                name: "IX_ChatPreviews_UserId",
                table: "ChatPreviews");

            migrationBuilder.AlterColumn<int>(
                name: "UnreadCount",
                table: "ChatPreviews",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    FriendshipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<int>(type: "int", nullable: false),
                    AddresseeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.FriendshipId);
                    table.ForeignKey(
                        name: "FK_Friendships_User_AddresseeId",
                        column: x => x.AddresseeId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Friendships_User_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatPreviews_UserId_OtherUserId",
                table: "ChatPreviews",
                columns: new[] { "UserId", "OtherUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId",
                table: "Friendships",
                column: "AddresseeId");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId_AddresseeId",
                table: "Friendships",
                columns: new[] { "RequesterId", "AddresseeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatPreviews_User_OtherUserId",
                table: "ChatPreviews",
                column: "OtherUserId",
                principalTable: "User",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatPreviews_User_UserId",
                table: "ChatPreviews",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatPreviews_User_OtherUserId",
                table: "ChatPreviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatPreviews_User_UserId",
                table: "ChatPreviews");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_ChatPreviews_UserId_OtherUserId",
                table: "ChatPreviews");

            migrationBuilder.AlterColumn<int>(
                name: "UnreadCount",
                table: "ChatPreviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatPreviews_UserId",
                table: "ChatPreviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatPreviews_User_OtherUserId",
                table: "ChatPreviews",
                column: "OtherUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatPreviews_User_UserId",
                table: "ChatPreviews",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
