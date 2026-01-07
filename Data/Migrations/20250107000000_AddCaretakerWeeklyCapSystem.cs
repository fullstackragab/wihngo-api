using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wihngo.Data.Migrations
{
    /// <summary>
    /// Adds the caretaker weekly cap system and love videos feature:
    /// - weekly_cap column to users table (how much a caretaker can RECEIVE per week in baseline support)
    /// - caretaker_support_receipts table for tracking baseline vs gift transactions
    /// - love_videos table for YouTube community submissions
    ///
    /// Invariant: Birds never multiply money. One user = one wallet = capped baseline support.
    /// Invariant: Love videos express love. Allocation ignores attention.
    /// </summary>
    public partial class AddCaretakerWeeklyCapSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add weekly_cap column to users table (default 5 USDC)
            migrationBuilder.AddColumn<decimal>(
                name: "weekly_cap",
                table: "users",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 5.00m);

            // Create caretaker_support_receipts table for tracking baseline vs gift transactions
            migrationBuilder.CreateTable(
                name: "caretaker_support_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caretaker_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tx_signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    week_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    support_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_on_chain = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caretaker_support_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_caretaker_support_receipts_users_caretaker_user_id",
                        column: x => x.caretaker_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_caretaker_support_receipts_users_supporter_user_id",
                        column: x => x.supporter_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_caretaker_support_receipts_birds_bird_id",
                        column: x => x.bird_id,
                        principalTable: "birds",
                        principalColumn: "bird_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_caretaker_support_receipts_support_intents_support_intent_id",
                        column: x => x.support_intent_id,
                        principalTable: "support_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes for efficient weekly cap calculations
            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_caretaker_user_id",
                table: "caretaker_support_receipts",
                column: "caretaker_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_supporter_user_id",
                table: "caretaker_support_receipts",
                column: "supporter_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_week_id",
                table: "caretaker_support_receipts",
                column: "week_id");

            // Composite index for weekly cap query: caretaker + week + type (baseline only)
            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_weekly_baseline",
                table: "caretaker_support_receipts",
                columns: new[] { "caretaker_user_id", "week_id", "transaction_type" });

            // Unique constraint on tx_signature to prevent duplicate records
            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_tx_signature_unique",
                table: "caretaker_support_receipts",
                column: "tx_signature",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_bird_id",
                table: "caretaker_support_receipts",
                column: "bird_id");

            migrationBuilder.CreateIndex(
                name: "ix_caretaker_support_receipts_created_at",
                table: "caretaker_support_receipts",
                column: "created_at");

            // ===========================================
            // LOVE VIDEOS TABLE
            // Love videos express love. Allocation ignores attention.
            // ===========================================
            migrationBuilder.CreateTable(
                name: "love_videos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    youtube_video_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    moderated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    // Direct media upload support (alternative to YouTube)
                    media_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    media_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    media_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_love_videos", x => x.id);
                    table.ForeignKey(
                        name: "FK_love_videos_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_love_videos_users_moderated_by_user_id",
                        column: x => x.moderated_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Love videos indexes
            migrationBuilder.CreateIndex(
                name: "ix_love_videos_status",
                table: "love_videos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_love_videos_category",
                table: "love_videos",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_love_videos_created_at",
                table: "love_videos",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_love_videos_youtube_video_id",
                table: "love_videos",
                column: "youtube_video_id");

            migrationBuilder.CreateIndex(
                name: "ix_love_videos_submitted_by_user_id",
                table: "love_videos",
                column: "submitted_by_user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the love_videos table
            migrationBuilder.DropTable(name: "love_videos");

            // Drop the caretaker_support_receipts table
            migrationBuilder.DropTable(name: "caretaker_support_receipts");

            // Drop the weekly_cap column from users
            migrationBuilder.DropColumn(name: "weekly_cap", table: "users");
        }
    }
}
