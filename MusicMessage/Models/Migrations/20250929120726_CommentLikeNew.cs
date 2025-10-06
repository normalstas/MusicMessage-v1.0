using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class CommentLikeNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_User_AuthorId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_User_UserId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_User_AuthorId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_PostShares_User_UserId",
                table: "PostShares");

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "PostComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CommentLikes",
                columns: table => new
                {
                    LikeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentLikes", x => x.LikeId);
                    table.ForeignKey(
                        name: "FK_CommentLikes_PostComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "PostComments",
                        principalColumn: "CommentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentLikes_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_CommentId_UserId",
                table: "CommentLikes",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_UserId",
                table: "CommentLikes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments",
                column: "ParentCommentId",
                principalTable: "PostComments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_User_AuthorId",
                table: "PostComments",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_User_UserId",
                table: "PostLikes",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_User_AuthorId",
                table: "Posts",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostShares_User_UserId",
                table: "PostShares",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_User_AuthorId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostLikes_User_UserId",
                table: "PostLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_User_AuthorId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_PostShares_User_UserId",
                table: "PostShares");

            migrationBuilder.DropTable(
                name: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "PostComments");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_PostComments_ParentCommentId",
                table: "PostComments",
                column: "ParentCommentId",
                principalTable: "PostComments",
                principalColumn: "CommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_User_AuthorId",
                table: "PostComments",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostLikes_User_UserId",
                table: "PostLikes",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_User_AuthorId",
                table: "Posts",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostShares_User_UserId",
                table: "PostShares",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
