using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumAndCharityEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("1092e093-1918-48c0-a120-16dc1eb05d70"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("7c4c55e3-ca02-4e90-b554-a537a74fa76c"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("a954c8c9-15ea-4545-a7b3-dd1481272df7"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("cd429602-84cc-4069-803c-f651c6d3ae17"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("e1f45915-f340-4d5d-b252-da1436e06cf9"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("ed756e42-9858-4a72-bb56-989b38555def"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("ff5a7387-2396-48aa-a474-aa89c3a2f8a8"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("cfb13d53-384d-4edf-b8e2-29c7c3c537e8"));

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "birds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_url",
                table: "birds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "charity_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charity_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    allocated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charity_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_charity_allocations_bird_premium_subscriptions_subscription",
                        column: x => x.subscription_id,
                        principalTable: "bird_premium_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "charity_impact_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_contributed = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    birds_helped = table.Column<int>(type: "integer", nullable: false),
                    shelters_supported = table.Column<int>(type: "integer", nullable: false),
                    conservation_projects = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charity_impact_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    preference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<int>(type: "integer", nullable: false),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    push_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.preference_id);
                    table.ForeignKey(
                        name: "fk_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_settings",
                columns: table => new
                {
                    settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiet_hours_start = table.Column<TimeSpan>(type: "interval", nullable: false),
                    quiet_hours_end = table.Column<TimeSpan>(type: "interval", nullable: false),
                    quiet_hours_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    max_push_per_day = table.Column<int>(type: "integer", nullable: false),
                    max_email_per_day = table.Column<int>(type: "integer", nullable: false),
                    enable_notification_grouping = table.Column<bool>(type: "boolean", nullable: false),
                    grouping_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    enable_daily_digest = table.Column<bool>(type: "boolean", nullable: false),
                    daily_digest_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_settings", x => x.settings_id);
                    table.ForeignKey(
                        name: "fk_notification_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    channels = table.Column<int>(type: "integer", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deep_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: true),
                    story_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_count = table.Column<int>(type: "integer", nullable: false),
                    push_sent = table.Column<bool>(type: "boolean", nullable: false),
                    email_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sms_sent = table.Column<bool>(type: "boolean", nullable: false),
                    push_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sms_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.notification_id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premium_styles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frame_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    badge_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    highlight_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    theme_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_premium_styles", x => x.id);
                    table.ForeignKey(
                        name: "fk_premium_styles_birds_bird_id",
                        column: x => x.bird_id,
                        principalTable: "birds",
                        principalColumn: "bird_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_devices",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    push_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_devices", x => x.device_id);
                    table.ForeignKey(
                        name: "fk_user_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("042d4663-44ac-40c6-82fc-329b6cba29e0"), "SOL", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(478), "coingecko", 100m },
                    { new Guid("29768fb7-037b-4da0-99e4-ff1ddb1e753c"), "USDT", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(469), "coingecko", 1m },
                    { new Guid("a3207aab-5001-4273-ab94-ff67504b7386"), "USDC", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(472), "coingecko", 1m },
                    { new Guid("b3be5e20-77eb-4140-a744-0a515f3a2113"), "DOGE", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(480), "coingecko", 0.1m },
                    { new Guid("b47fb181-e5ef-44aa-9edc-20b55f58205c"), "BNB", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(474), "coingecko", 500m },
                    { new Guid("e91cde8e-e389-401c-89a6-43bc056beebf"), "BTC", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(452), "coingecko", 50000m },
                    { new Guid("fadaeb59-097b-484f-9b48-f4eee141fa83"), "ETH", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(467), "coingecko", 3000m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[,]
                {
                    { new Guid("1ae09ada-105b-4d2b-a255-768374709e85"), "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(316), "USDT", null, true, "ethereum", null, new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(316) },
                    { new Guid("35d4c7d4-ee68-47c1-9ff1-a449fa7da91d"), "0x83675000ac9915614afff618906421a2baea0020", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(320), "USDT", null, true, "binance-smart-chain", null, new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(320) },
                    { new Guid("a989ec71-d964-4e85-a757-a20d607d4917"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(311), "USDT", null, true, "tron", null, new DateTime(2025, 12, 11, 12, 10, 21, 606, DateTimeKind.Utc).AddTicks(312) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_charity_allocations_allocated_at",
                table: "charity_allocations",
                column: "allocated_at");

            migrationBuilder.CreateIndex(
                name: "ix_charity_allocations_subscription_id",
                table: "charity_allocations",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_user_id_notification_type",
                table: "notification_preferences",
                columns: new[] { "user_id", "notification_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_settings_user_id",
                table: "notification_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_group_id",
                table: "notifications",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_premium_styles_bird_id",
                table: "premium_styles",
                column: "bird_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_devices_push_token",
                table: "user_devices",
                column: "push_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_devices_user_id",
                table: "user_devices",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "charity_allocations");

            migrationBuilder.DropTable(
                name: "charity_impact_stats");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_settings");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "premium_styles");

            migrationBuilder.DropTable(
                name: "user_devices");

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("042d4663-44ac-40c6-82fc-329b6cba29e0"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("29768fb7-037b-4da0-99e4-ff1ddb1e753c"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("a3207aab-5001-4273-ab94-ff67504b7386"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("b3be5e20-77eb-4140-a744-0a515f3a2113"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("b47fb181-e5ef-44aa-9edc-20b55f58205c"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("e91cde8e-e389-401c-89a6-43bc056beebf"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("fadaeb59-097b-484f-9b48-f4eee141fa83"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("1ae09ada-105b-4d2b-a255-768374709e85"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("35d4c7d4-ee68-47c1-9ff1-a449fa7da91d"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("a989ec71-d964-4e85-a757-a20d607d4917"));

            migrationBuilder.DropColumn(
                name: "video_url",
                table: "birds");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "birds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("1092e093-1918-48c0-a120-16dc1eb05d70"), "BTC", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3205), "coingecko", 50000m },
                    { new Guid("7c4c55e3-ca02-4e90-b554-a537a74fa76c"), "USDT", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3214), "coingecko", 1m },
                    { new Guid("a954c8c9-15ea-4545-a7b3-dd1481272df7"), "DOGE", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3243), "coingecko", 0.1m },
                    { new Guid("cd429602-84cc-4069-803c-f651c6d3ae17"), "ETH", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3211), "coingecko", 3000m },
                    { new Guid("e1f45915-f340-4d5d-b252-da1436e06cf9"), "BNB", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3232), "coingecko", 500m },
                    { new Guid("ed756e42-9858-4a72-bb56-989b38555def"), "USDC", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3229), "coingecko", 1m },
                    { new Guid("ff5a7387-2396-48aa-a474-aa89c3a2f8a8"), "SOL", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(3236), "coingecko", 100m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[] { new Guid("cfb13d53-384d-4edf-b8e2-29c7c3c537e8"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(2873), "USDT", null, true, "tron", null, new DateTime(2025, 12, 10, 1, 1, 1, 277, DateTimeKind.Utc).AddTicks(2873) });
        }
    }
}
