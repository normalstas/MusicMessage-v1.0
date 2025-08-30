using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class ReactUp2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reaction_Message_MessageId",
                table: "Reactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reaction_User_UserId",
                table: "Reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reaction",
                table: "Reactions");

            migrationBuilder.RenameTable(
                name: "Reactions",
                newName: "Reactions");

            migrationBuilder.RenameIndex(
                name: "IX_Reaction_UserId",
                table: "Reactions",
                newName: "IX_Reactions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reaction_MessageId",
                table: "Reactions",
                newName: "IX_Reactions_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions",
                column: "ReactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Message_MessageId",
                table: "Reactions",
                column: "MessageId",
                principalTable: "Message",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_User_UserId",
                table: "Reactions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Message_MessageId",
                table: "Reactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_User_UserId",
                table: "Reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions");

            migrationBuilder.RenameTable(
                name: "Reactions",
                newName: "Reaction");

            migrationBuilder.RenameIndex(
                name: "IX_Reactions_UserId",
                table: "Reaction",
                newName: "IX_Reaction_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reactions_MessageId",
                table: "Reaction",
                newName: "IX_Reaction_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reaction",
                table: "Reaction",
                column: "ReactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reaction_Message_MessageId",
                table: "Reaction",
                column: "MessageId",
                principalTable: "Message",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reaction_User_UserId",
                table: "Reaction",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
