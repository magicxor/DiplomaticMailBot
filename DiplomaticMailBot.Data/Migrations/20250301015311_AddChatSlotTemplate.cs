using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaticMailBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSlotTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotTemplateId",
                table: "RegisteredChats",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredChats_SlotTemplateId",
                table: "RegisteredChats",
                column: "SlotTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegisteredChats_SlotTemplates_SlotTemplateId",
                table: "RegisteredChats",
                column: "SlotTemplateId",
                principalTable: "SlotTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegisteredChats_SlotTemplates_SlotTemplateId",
                table: "RegisteredChats");

            migrationBuilder.DropIndex(
                name: "IX_RegisteredChats_SlotTemplateId",
                table: "RegisteredChats");

            migrationBuilder.DropColumn(
                name: "SlotTemplateId",
                table: "RegisteredChats");
        }
    }
}
