using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaticMailBot.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameSlotInstanceChatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlotInstances_RegisteredChats_FromChatId",
                table: "SlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotInstances_RegisteredChats_ToChatId",
                table: "SlotInstances");

            migrationBuilder.RenameIndex(
                name: "DiplomaticMailPoll_SlotInstanceId_Unique_IX",
                table: "SlotPolls",
                newName: "SlotPoll_SlotInstanceId_Unique_IX");

            migrationBuilder.RenameColumn(
                name: "ToChatId",
                table: "SlotInstances",
                newName: "TargetChatId");

            migrationBuilder.RenameColumn(
                name: "FromChatId",
                table: "SlotInstances",
                newName: "SourceChatId");

            migrationBuilder.RenameIndex(
                name: "IX_SlotInstances_ToChatId",
                table: "SlotInstances",
                newName: "IX_SlotInstances_TargetChatId");

            migrationBuilder.RenameIndex(
                name: "IX_SlotInstances_FromChatId",
                table: "SlotInstances",
                newName: "IX_SlotInstances_SourceChatId");

            migrationBuilder.RenameIndex(
                name: "DiplomaticMailOutbox_Status_IX",
                table: "MessageOutbox",
                newName: "MessageOutbox_Status_IX");

            migrationBuilder.RenameIndex(
                name: "DiplomaticMailOutbox_SlotInstanceId_IX",
                table: "MessageOutbox",
                newName: "MessageOutbox_SlotInstanceId_IX");

            migrationBuilder.RenameIndex(
                name: "DiplomaticMailCandidate_Unique_IX",
                table: "MessageCandidates",
                newName: "MessageCandidate_Unique_IX");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotInstances_RegisteredChats_SourceChatId",
                table: "SlotInstances",
                column: "SourceChatId",
                principalTable: "RegisteredChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotInstances_RegisteredChats_TargetChatId",
                table: "SlotInstances",
                column: "TargetChatId",
                principalTable: "RegisteredChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlotInstances_RegisteredChats_SourceChatId",
                table: "SlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotInstances_RegisteredChats_TargetChatId",
                table: "SlotInstances");

            migrationBuilder.RenameIndex(
                name: "SlotPoll_SlotInstanceId_Unique_IX",
                table: "SlotPolls",
                newName: "DiplomaticMailPoll_SlotInstanceId_Unique_IX");

            migrationBuilder.RenameColumn(
                name: "TargetChatId",
                table: "SlotInstances",
                newName: "ToChatId");

            migrationBuilder.RenameColumn(
                name: "SourceChatId",
                table: "SlotInstances",
                newName: "FromChatId");

            migrationBuilder.RenameIndex(
                name: "IX_SlotInstances_TargetChatId",
                table: "SlotInstances",
                newName: "IX_SlotInstances_ToChatId");

            migrationBuilder.RenameIndex(
                name: "IX_SlotInstances_SourceChatId",
                table: "SlotInstances",
                newName: "IX_SlotInstances_FromChatId");

            migrationBuilder.RenameIndex(
                name: "MessageOutbox_Status_IX",
                table: "MessageOutbox",
                newName: "DiplomaticMailOutbox_Status_IX");

            migrationBuilder.RenameIndex(
                name: "MessageOutbox_SlotInstanceId_IX",
                table: "MessageOutbox",
                newName: "DiplomaticMailOutbox_SlotInstanceId_IX");

            migrationBuilder.RenameIndex(
                name: "MessageCandidate_Unique_IX",
                table: "MessageCandidates",
                newName: "DiplomaticMailCandidate_Unique_IX");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotInstances_RegisteredChats_FromChatId",
                table: "SlotInstances",
                column: "FromChatId",
                principalTable: "RegisteredChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotInstances_RegisteredChats_ToChatId",
                table: "SlotInstances",
                column: "ToChatId",
                principalTable: "RegisteredChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
