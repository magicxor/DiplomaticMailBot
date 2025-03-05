using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiplomaticMailBot.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegisteredChats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    ChatTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChatAlias = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredChats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlotTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VoteStartAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    VoteEndAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiplomaticRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceChatId = table.Column<int>(type: "integer", nullable: false),
                    TargetChatId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaticRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaticRelations_RegisteredChats_SourceChatId",
                        column: x => x.SourceChatId,
                        principalTable: "RegisteredChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiplomaticRelations_RegisteredChats_TargetChatId",
                        column: x => x.TargetChatId,
                        principalTable: "RegisteredChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlotInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    FromChatId = table.Column<int>(type: "integer", nullable: false),
                    ToChatId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotInstances_RegisteredChats_FromChatId",
                        column: x => x.FromChatId,
                        principalTable: "RegisteredChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlotInstances_RegisteredChats_ToChatId",
                        column: x => x.ToChatId,
                        principalTable: "RegisteredChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlotInstances_SlotTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SlotTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiplomaticMailCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    Preview = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubmitterId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotInstanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaticMailCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaticMailCandidates_SlotInstances_SlotInstanceId",
                        column: x => x.SlotInstanceId,
                        principalTable: "SlotInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiplomaticMailPolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlotInstanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaticMailPolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaticMailPolls_SlotInstances_SlotInstanceId",
                        column: x => x.SlotInstanceId,
                        principalTable: "SlotInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiplomaticMailOutbox",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StatusDetails = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlotInstanceId = table.Column<int>(type: "integer", nullable: false),
                    DiplomaticMailCandidateId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiplomaticMailOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiplomaticMailOutbox_DiplomaticMailCandidates_DiplomaticMai~",
                        column: x => x.DiplomaticMailCandidateId,
                        principalTable: "DiplomaticMailCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiplomaticMailOutbox_SlotInstances_SlotInstanceId",
                        column: x => x.SlotInstanceId,
                        principalTable: "SlotInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "DiplomaticMailCandidate_Unique_IX",
                table: "DiplomaticMailCandidates",
                columns: new[] { "MessageId", "SlotInstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaticMailCandidates_SlotInstanceId",
                table: "DiplomaticMailCandidates",
                column: "SlotInstanceId");

            migrationBuilder.CreateIndex(
                name: "DiplomaticMailOutbox_SlotInstanceId_IX",
                table: "DiplomaticMailOutbox",
                column: "SlotInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "DiplomaticMailOutbox_Status_IX",
                table: "DiplomaticMailOutbox",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaticMailOutbox_DiplomaticMailCandidateId",
                table: "DiplomaticMailOutbox",
                column: "DiplomaticMailCandidateId");

            migrationBuilder.CreateIndex(
                name: "DiplomaticMailPoll_SlotInstanceId_Unique_IX",
                table: "DiplomaticMailPolls",
                column: "SlotInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "DiplomaticRelation_Unique_IX",
                table: "DiplomaticRelations",
                columns: new[] { "SourceChatId", "TargetChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiplomaticRelations_TargetChatId",
                table: "DiplomaticRelations",
                column: "TargetChatId");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredChats_IsDeleted",
                table: "RegisteredChats",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "RegisteredChat_ChatAlias_Unique_IX",
                table: "RegisteredChats",
                column: "ChatAlias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RegisteredChat_ChatId_Unique_IX",
                table: "RegisteredChats",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlotInstances_FromChatId",
                table: "SlotInstances",
                column: "FromChatId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotInstances_Status",
                table: "SlotInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SlotInstances_TemplateId",
                table: "SlotInstances",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotInstances_ToChatId",
                table: "SlotInstances",
                column: "ToChatId");

            migrationBuilder.CreateIndex(
                name: "SlotInstance_Unique_IX",
                table: "SlotInstances",
                columns: new[] { "Date", "TemplateId", "FromChatId", "ToChatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiplomaticMailOutbox");

            migrationBuilder.DropTable(
                name: "DiplomaticMailPolls");

            migrationBuilder.DropTable(
                name: "DiplomaticRelations");

            migrationBuilder.DropTable(
                name: "DiplomaticMailCandidates");

            migrationBuilder.DropTable(
                name: "SlotInstances");

            migrationBuilder.DropTable(
                name: "RegisteredChats");

            migrationBuilder.DropTable(
                name: "SlotTemplates");
        }
    }
}
