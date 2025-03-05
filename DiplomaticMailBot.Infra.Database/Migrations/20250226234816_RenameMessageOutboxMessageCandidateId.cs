using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaticMailBot.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameMessageOutboxMessageCandidateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_DiplomaticMailCandidateId",
                table: "MessageOutbox");

            migrationBuilder.RenameColumn(
                name: "DiplomaticMailCandidateId",
                table: "MessageOutbox",
                newName: "MessageCandidateId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageOutbox_DiplomaticMailCandidateId",
                table: "MessageOutbox",
                newName: "IX_MessageOutbox_MessageCandidateId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_MessageCandidateId",
                table: "MessageOutbox",
                column: "MessageCandidateId",
                principalTable: "MessageCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_MessageCandidateId",
                table: "MessageOutbox");

            migrationBuilder.RenameColumn(
                name: "MessageCandidateId",
                table: "MessageOutbox",
                newName: "DiplomaticMailCandidateId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageOutbox_MessageCandidateId",
                table: "MessageOutbox",
                newName: "IX_MessageOutbox_DiplomaticMailCandidateId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_DiplomaticMailCandidateId",
                table: "MessageOutbox",
                column: "DiplomaticMailCandidateId",
                principalTable: "MessageCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
