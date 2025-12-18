using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartFeedSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add language and country columns to stories table
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "stories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "stories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // Add preferred_languages and country columns to users table
            migrationBuilder.AddColumn<string>(
                name: "preferred_languages_json",
                table: "users",
                type: "jsonb",
                nullable: true,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // Create user_feed_interactions table
            migrationBuilder.CreateTable(
                name: "user_feed_interactions",
                columns: table => new
                {
                    interaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    view_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_feed_interactions", x => x.interaction_id);
                    table.ForeignKey(
                        name: "FK_user_feed_interactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_feed_interactions_stories_story_id",
                        column: x => x.story_id,
                        principalTable: "stories",
                        principalColumn: "story_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create user_bird_follows table
            migrationBuilder.CreateTable(
                name: "user_bird_follows",
                columns: table => new
                {
                    follow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bird_follows", x => x.follow_id);
                    table.ForeignKey(
                        name: "FK_user_bird_follows_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_bird_follows_birds_bird_id",
                        column: x => x.bird_id,
                        principalTable: "birds",
                        principalColumn: "bird_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for stories table
            migrationBuilder.CreateIndex(
                name: "ix_stories_language",
                table: "stories",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_stories_country",
                table: "stories",
                column: "country");

            // Create indexes for users table
            migrationBuilder.CreateIndex(
                name: "ix_users_country",
                table: "users",
                column: "country");

            // Create indexes for user_feed_interactions table
            migrationBuilder.CreateIndex(
                name: "ix_user_feed_interactions_user_id",
                table: "user_feed_interactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feed_interactions_story_id",
                table: "user_feed_interactions",
                column: "story_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_feed_interactions_user_type",
                table: "user_feed_interactions",
                columns: new[] { "user_id", "interaction_type" });

            migrationBuilder.CreateIndex(
                name: "ix_user_feed_interactions_created_at",
                table: "user_feed_interactions",
                column: "created_at");

            // Create indexes for user_bird_follows table
            migrationBuilder.CreateIndex(
                name: "ix_user_bird_follows_user_id",
                table: "user_bird_follows",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_bird_follows_bird_id",
                table: "user_bird_follows",
                column: "bird_id");

            // Create unique constraint for user_bird_follows
            migrationBuilder.CreateIndex(
                name: "ix_user_bird_follows_user_bird_unique",
                table: "user_bird_follows",
                columns: new[] { "user_id", "bird_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first
            migrationBuilder.DropIndex(name: "ix_stories_language", table: "stories");
            migrationBuilder.DropIndex(name: "ix_stories_country", table: "stories");
            migrationBuilder.DropIndex(name: "ix_users_country", table: "users");

            // Drop tables
            migrationBuilder.DropTable(name: "user_feed_interactions");
            migrationBuilder.DropTable(name: "user_bird_follows");

            // Drop columns from stories
            migrationBuilder.DropColumn(name: "language", table: "stories");
            migrationBuilder.DropColumn(name: "country", table: "stories");

            // Drop columns from users
            migrationBuilder.DropColumn(name: "preferred_languages_json", table: "users");
            migrationBuilder.DropColumn(name: "country", table: "users");
        }
    }
}
