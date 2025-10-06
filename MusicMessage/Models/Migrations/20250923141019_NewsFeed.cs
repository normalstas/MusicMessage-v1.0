using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class NewsFeed : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Posts",
				columns: table => new
				{
					PostId = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					AuthorId = table.Column<int>(type: "int", nullable: false),
					Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
					ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
					VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
					CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
					LikesCount = table.Column<int>(type: "int", nullable: false),
					CommentsCount = table.Column<int>(type: "int", nullable: false),
					SharesCount = table.Column<int>(type: "int", nullable: false),
					IsPublic = table.Column<bool>(type: "bit", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Posts", x => x.PostId);
					table.ForeignKey(
						name: "FK_Posts_User_AuthorId",
						column: x => x.AuthorId,
						principalTable: "User",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict); 
				});

			migrationBuilder.CreateTable(
				name: "PostComments",
				columns: table => new
				{
					CommentId = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					PostId = table.Column<int>(type: "int", nullable: false),
					AuthorId = table.Column<int>(type: "int", nullable: false),
					Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
					CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
					ParentCommentId = table.Column<int>(type: "int", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PostComments", x => x.CommentId);
					table.ForeignKey(
						name: "FK_PostComments_PostComments_ParentCommentId",
						column: x => x.ParentCommentId,
						principalTable: "PostComments",
						principalColumn: "CommentId",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_PostComments_Posts_PostId",
						column: x => x.PostId,
						principalTable: "Posts",
						principalColumn: "PostId",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_PostComments_User_AuthorId",
						column: x => x.AuthorId,
						principalTable: "User",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict); 
				});

			migrationBuilder.CreateTable(
				name: "PostLikes",
				columns: table => new
				{
					LikeId = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					PostId = table.Column<int>(type: "int", nullable: false),
					UserId = table.Column<int>(type: "int", nullable: false),
					LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PostLikes", x => x.LikeId);
					table.ForeignKey(
						name: "FK_PostLikes_Posts_PostId",
						column: x => x.PostId,
						principalTable: "Posts",
						principalColumn: "PostId",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_PostLikes_User_UserId",
						column: x => x.UserId,
						principalTable: "User",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "PostShares",
				columns: table => new
				{
					ShareId = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					PostId = table.Column<int>(type: "int", nullable: false),
					UserId = table.Column<int>(type: "int", nullable: false),
					SharedComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
					SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PostShares", x => x.ShareId);
					table.ForeignKey(
						name: "FK_PostShares_Posts_PostId",
						column: x => x.PostId,
						principalTable: "Posts",
						principalColumn: "PostId",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_PostShares_User_UserId",
						column: x => x.UserId,
						principalTable: "User",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_PostComments_AuthorId",
				table: "PostComments",
				column: "AuthorId");

			migrationBuilder.CreateIndex(
				name: "IX_PostComments_ParentCommentId",
				table: "PostComments",
				column: "ParentCommentId");

			migrationBuilder.CreateIndex(
				name: "IX_PostComments_PostId",
				table: "PostComments",
				column: "PostId");

			migrationBuilder.CreateIndex(
				name: "IX_PostLikes_PostId_UserId",
				table: "PostLikes",
				columns: new[] { "PostId", "UserId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_PostLikes_UserId",
				table: "PostLikes",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_Posts_AuthorId",
				table: "Posts",
				column: "AuthorId");

			migrationBuilder.CreateIndex(
				name: "IX_PostShares_PostId",
				table: "PostShares",
				column: "PostId");

			migrationBuilder.CreateIndex(
				name: "IX_PostShares_UserId",
				table: "PostShares",
				column: "UserId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "PostComments");

			migrationBuilder.DropTable(
				name: "PostLikes");

			migrationBuilder.DropTable(
				name: "PostShares");

			migrationBuilder.DropTable(
				name: "Posts");
		}
	}
}
