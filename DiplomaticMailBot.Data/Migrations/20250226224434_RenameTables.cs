using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaticMailBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaticMailCandidates_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailCandidates");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaticMailOutbox_DiplomaticMailCandidates_DiplomaticMai~",
                table: "DiplomaticMailOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaticMailOutbox_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_DiplomaticMailPolls_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailPolls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiplomaticMailPolls",
                table: "DiplomaticMailPolls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiplomaticMailOutbox",
                table: "DiplomaticMailOutbox");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiplomaticMailCandidates",
                table: "DiplomaticMailCandidates");

            migrationBuilder.RenameTable(
                name: "DiplomaticMailPolls",
                newName: "SlotPolls");

            migrationBuilder.RenameTable(
                name: "DiplomaticMailOutbox",
                newName: "MessageOutbox");

            migrationBuilder.RenameTable(
                name: "DiplomaticMailCandidates",
                newName: "MessageCandidates");

            migrationBuilder.RenameIndex(
                name: "IX_DiplomaticMailOutbox_DiplomaticMailCandidateId",
                table: "MessageOutbox",
                newName: "IX_MessageOutbox_DiplomaticMailCandidateId");

            migrationBuilder.RenameIndex(
                name: "IX_DiplomaticMailCandidates_SlotInstanceId",
                table: "MessageCandidates",
                newName: "IX_MessageCandidates_SlotInstanceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SlotPolls",
                table: "SlotPolls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageOutbox",
                table: "MessageOutbox",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageCandidates",
                table: "MessageCandidates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageCandidates_SlotInstances_SlotInstanceId",
                table: "MessageCandidates",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_DiplomaticMailCandidateId",
                table: "MessageOutbox",
                column: "DiplomaticMailCandidateId",
                principalTable: "MessageCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MessageOutbox_SlotInstances_SlotInstanceId",
                table: "MessageOutbox",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotPolls_SlotInstances_SlotInstanceId",
                table: "SlotPolls",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageCandidates_SlotInstances_SlotInstanceId",
                table: "MessageCandidates");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageOutbox_MessageCandidates_DiplomaticMailCandidateId",
                table: "MessageOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageOutbox_SlotInstances_SlotInstanceId",
                table: "MessageOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotPolls_SlotInstances_SlotInstanceId",
                table: "SlotPolls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SlotPolls",
                table: "SlotPolls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageOutbox",
                table: "MessageOutbox");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageCandidates",
                table: "MessageCandidates");

            migrationBuilder.RenameTable(
                name: "SlotPolls",
                newName: "DiplomaticMailPolls");

            migrationBuilder.RenameTable(
                name: "MessageOutbox",
                newName: "DiplomaticMailOutbox");

            migrationBuilder.RenameTable(
                name: "MessageCandidates",
                newName: "DiplomaticMailCandidates");

            migrationBuilder.RenameIndex(
                name: "IX_MessageOutbox_DiplomaticMailCandidateId",
                table: "DiplomaticMailOutbox",
                newName: "IX_DiplomaticMailOutbox_DiplomaticMailCandidateId");

            migrationBuilder.RenameIndex(
                name: "IX_MessageCandidates_SlotInstanceId",
                table: "DiplomaticMailCandidates",
                newName: "IX_DiplomaticMailCandidates_SlotInstanceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiplomaticMailPolls",
                table: "DiplomaticMailPolls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiplomaticMailOutbox",
                table: "DiplomaticMailOutbox",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiplomaticMailCandidates",
                table: "DiplomaticMailCandidates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaticMailCandidates_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailCandidates",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaticMailOutbox_DiplomaticMailCandidates_DiplomaticMai~",
                table: "DiplomaticMailOutbox",
                column: "DiplomaticMailCandidateId",
                principalTable: "DiplomaticMailCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaticMailOutbox_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailOutbox",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiplomaticMailPolls_SlotInstances_SlotInstanceId",
                table: "DiplomaticMailPolls",
                column: "SlotInstanceId",
                principalTable: "SlotInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
