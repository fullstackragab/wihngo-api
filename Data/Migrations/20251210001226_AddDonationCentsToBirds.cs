using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationCentsToBirds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "highlight_order",
                table: "stories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_highlighted",
                table: "stories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "donation_cents",
                table: "birds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_premium",
                table: "birds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_media_count",
                table: "birds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "premium_expires_at",
                table: "birds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "premium_plan",
                table: "birds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "premium_style_json",
                table: "birds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "qr_code_url",
                table: "birds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bird_premium_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    provider_subscription_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    price_cents = table.Column<long>(type: "bigint", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bird_premium_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_bird_premium_subscriptions_birds_bird_id",
                        column: x => x.bird_id,
                        principalTable: "birds",
                        principalColumn: "bird_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bird_premium_subscriptions_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crypto_exchange_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    usd_rate = table.Column<decimal>(type: "numeric(20,2)", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_exchange_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crypto_payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crypto_payment_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    amount_crypto = table.Column<decimal>(type: "numeric(20,10)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(20,2)", nullable: false),
                    wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    qr_code_data = table.Column<string>(type: "text", nullable: false),
                    payment_uri = table.Column<string>(type: "text", nullable: false),
                    transaction_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    confirmations = table.Column<int>(type: "integer", nullable: false),
                    required_confirmations = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_payment_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    private_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    derivation_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_wallets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crypto_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(20,10)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    confirmations = table.Column<int>(type: "integer", nullable: false),
                    block_number = table.Column<long>(type: "bigint", nullable: true),
                    block_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fee = table.Column<decimal>(type: "numeric(20,10)", nullable: true),
                    gas_used = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    raw_transaction = table.Column<string>(type: "jsonb", nullable: true),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_crypto_transactions_crypto_payment_requests_payment_request~",
                        column: x => x.payment_request_id,
                        principalTable: "crypto_payment_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("59b7b668-b1ba-40b5-be17-4bbb6d69cd72"), "BTC", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(572), "coingecko", 50000m },
                    { new Guid("5a2cca79-293b-4863-a1e3-e8f3be525bfe"), "SOL", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(589), "coingecko", 100m },
                    { new Guid("78211331-477d-4399-9bb2-e63c2e939852"), "BNB", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(587), "coingecko", 500m },
                    { new Guid("7bac845b-a02a-4d7d-83ea-0727898e73d4"), "DOGE", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(591), "coingecko", 0.1m },
                    { new Guid("b70e13d4-4c36-4848-82b7-250952e56422"), "USDT", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(580), "coingecko", 1m },
                    { new Guid("bfb4bc3e-b936-41c8-a7d8-120944b066c9"), "ETH", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(577), "coingecko", 3000m },
                    { new Guid("dd0ee405-6005-4a5c-ad8b-e243c375be00"), "USDC", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(585), "coingecko", 1m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[] { new Guid("1a0b9e7d-73b7-436d-8b46-780a3b5b35f8"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(434), "USDT", null, true, "tron", null, new DateTime(2025, 12, 10, 0, 12, 25, 874, DateTimeKind.Utc).AddTicks(435) });

            migrationBuilder.CreateIndex(
                name: "IX_bird_premium_subscriptions_bird_id",
                table: "bird_premium_subscriptions",
                column: "bird_id");

            migrationBuilder.CreateIndex(
                name: "IX_bird_premium_subscriptions_owner_id",
                table: "bird_premium_subscriptions",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_exchange_rates_currency",
                table: "crypto_exchange_rates",
                column: "currency",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crypto_payment_methods_user_id_wallet_address_currency_netw~",
                table: "crypto_payment_methods",
                columns: new[] { "user_id", "wallet_address", "currency", "network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crypto_payment_requests_expires_at",
                table: "crypto_payment_requests",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_payment_requests_status",
                table: "crypto_payment_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_payment_requests_transaction_hash",
                table: "crypto_payment_requests",
                column: "transaction_hash");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_transactions_payment_request_id",
                table: "crypto_transactions",
                column: "payment_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_transactions_transaction_hash",
                table: "crypto_transactions",
                column: "transaction_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_wallets_currency_network_address",
                table: "platform_wallets",
                columns: new[] { "currency", "network", "address" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bird_premium_subscriptions");

            migrationBuilder.DropTable(
                name: "crypto_exchange_rates");

            migrationBuilder.DropTable(
                name: "crypto_payment_methods");

            migrationBuilder.DropTable(
                name: "crypto_transactions");

            migrationBuilder.DropTable(
                name: "platform_wallets");

            migrationBuilder.DropTable(
                name: "crypto_payment_requests");

            migrationBuilder.DropColumn(
                name: "highlight_order",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "is_highlighted",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "donation_cents",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "is_premium",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "max_media_count",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "premium_expires_at",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "premium_plan",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "premium_style_json",
                table: "birds");

            migrationBuilder.DropColumn(
                name: "qr_code_url",
                table: "birds");
        }
    }
}
