using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicMessage.Models.Migrations
{
    /// <inheritdoc />
    public partial class InstalforChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.CreateTable(
		name: "ChatSettings",
		columns: table => new
		{
			ChatSettingsId = table.Column<int>(type: "int", nullable: false)
				.Annotation("SqlServer:Identity", "1, 1"),
			UserId = table.Column<int>(type: "int", nullable: false),
			NotificationsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
			SoundEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
			Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Light")
		},
		constraints: table =>
		{
			table.PrimaryKey("PK_ChatSettings", x => x.ChatSettingsId);
			table.ForeignKey(
				name: "FK_ChatSettings_User_UserId",
				column: x => x.UserId,
				principalTable: "User",
				principalColumn: "UserId",
				onDelete: ReferentialAction.Cascade);
		});

			migrationBuilder.CreateIndex(
				name: "IX_ChatSettings_UserId",
				table: "ChatSettings",
				column: "UserId",
				unique: true);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropTable(
			name: "ChatSettings");
		}
    }
}
