using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryHighlightFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bird_premium_subscriptions_birds_bird_id",
                table: "bird_premium_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_bird_premium_subscriptions_users_owner_id",
                table: "bird_premium_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_birds_users_owner_id",
                table: "birds");

            migrationBuilder.DropForeignKey(
                name: "FK_crypto_transactions_crypto_payment_requests_payment_request~",
                table: "crypto_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_loves_birds_bird_id",
                table: "loves");

            migrationBuilder.DropForeignKey(
                name: "FK_loves_users_user_id",
                table: "loves");

            migrationBuilder.DropForeignKey(
                name: "FK_stories_birds_bird_id",
                table: "stories");

            migrationBuilder.DropForeignKey(
                name: "FK_stories_users_author_id",
                table: "stories");

            migrationBuilder.DropForeignKey(
                name: "FK_support_transactions_birds_bird_id",
                table: "support_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_support_transactions_users_supporter_id",
                table: "support_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_support_usage_birds_bird_id",
                table: "support_usage");

            migrationBuilder.DropForeignKey(
                name: "FK_support_usage_users_reported_by",
                table: "support_usage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_support_usage",
                table: "support_usage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_support_transactions",
                table: "support_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stories",
                table: "stories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_platform_wallets",
                table: "platform_wallets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_loves",
                table: "loves");

            migrationBuilder.DropPrimaryKey(
                name: "PK_crypto_transactions",
                table: "crypto_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_crypto_payment_requests",
                table: "crypto_payment_requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_crypto_payment_methods",
                table: "crypto_payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_crypto_exchange_rates",
                table: "crypto_exchange_rates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_birds",
                table: "birds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bird_premium_subscriptions",
                table: "bird_premium_subscriptions");

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("26106c54-99bb-4f9a-81f5-9845a95a05ec"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("269e39df-8fc8-4b37-9b37-5ee3491ea04e"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("2ab74aaa-79b0-48ac-b463-1c31631f8ff9"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("3b8b5ffc-8dde-4a0f-b2d9-f9f356c528ee"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("7df322cb-48c3-445f-8380-5c52ee6a0ec4"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("92860bc0-7342-4749-ac2d-0727f0264b4e"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("b3ab45ce-c2ee-4a74-8feb-70ee37d86071"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("549b7eb3-329e-4c25-8a0f-1774b3bad08b"));

            migrationBuilder.RenameIndex(
                name: "IX_support_usage_reported_by",
                table: "support_usage",
                newName: "ix_support_usage_reported_by");

            migrationBuilder.RenameIndex(
                name: "IX_support_usage_bird_id",
                table: "support_usage",
                newName: "ix_support_usage_bird_id");

            migrationBuilder.RenameIndex(
                name: "IX_support_transactions_supporter_id",
                table: "support_transactions",
                newName: "ix_support_transactions_supporter_id");

            migrationBuilder.RenameIndex(
                name: "IX_support_transactions_bird_id",
                table: "support_transactions",
                newName: "ix_support_transactions_bird_id");

            migrationBuilder.RenameIndex(
                name: "IX_stories_bird_id",
                table: "stories",
                newName: "ix_stories_bird_id");

            migrationBuilder.RenameIndex(
                name: "IX_stories_author_id",
                table: "stories",
                newName: "ix_stories_author_id");

            migrationBuilder.RenameIndex(
                name: "IX_platform_wallets_currency_network_address",
                table: "platform_wallets",
                newName: "ix_platform_wallets_currency_network_address");

            migrationBuilder.RenameIndex(
                name: "IX_loves_bird_id",
                table: "loves",
                newName: "ix_loves_bird_id");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_transactions_transaction_hash",
                table: "crypto_transactions",
                newName: "ix_crypto_transactions_transaction_hash");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_transactions_payment_request_id",
                table: "crypto_transactions",
                newName: "ix_crypto_transactions_payment_request_id");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_payment_requests_transaction_hash",
                table: "crypto_payment_requests",
                newName: "ix_crypto_payment_requests_transaction_hash");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_payment_requests_status",
                table: "crypto_payment_requests",
                newName: "ix_crypto_payment_requests_status");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_payment_requests_expires_at",
                table: "crypto_payment_requests",
                newName: "ix_crypto_payment_requests_expires_at");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_payment_methods_user_id_wallet_address_currency_netw~",
                table: "crypto_payment_methods",
                newName: "ix_crypto_payment_methods_user_id_wallet_address_currency_netw");

            migrationBuilder.RenameIndex(
                name: "IX_crypto_exchange_rates_currency",
                table: "crypto_exchange_rates",
                newName: "ix_crypto_exchange_rates_currency");

            migrationBuilder.RenameIndex(
                name: "IX_birds_owner_id",
                table: "birds",
                newName: "ix_birds_owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_bird_premium_subscriptions_owner_id",
                table: "bird_premium_subscriptions",
                newName: "ix_bird_premium_subscriptions_owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_bird_premium_subscriptions_bird_id",
                table: "bird_premium_subscriptions",
                newName: "ix_bird_premium_subscriptions_bird_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_support_usage",
                table: "support_usage",
                column: "usage_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_support_transactions",
                table: "support_transactions",
                column: "transaction_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stories",
                table: "stories",
                column: "story_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_platform_wallets",
                table: "platform_wallets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_loves",
                table: "loves",
                columns: new[] { "user_id", "bird_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_crypto_transactions",
                table: "crypto_transactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_crypto_payment_requests",
                table: "crypto_payment_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_crypto_payment_methods",
                table: "crypto_payment_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_crypto_exchange_rates",
                table: "crypto_exchange_rates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_birds",
                table: "birds",
                column: "bird_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bird_premium_subscriptions",
                table: "bird_premium_subscriptions",
                column: "id");

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

            migrationBuilder.AddForeignKey(
                name: "fk_bird_premium_subscriptions_birds_bird_id",
                table: "bird_premium_subscriptions",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_bird_premium_subscriptions_users_owner_id",
                table: "bird_premium_subscriptions",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_birds_users_owner_id",
                table: "birds",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_crypto_transactions_crypto_payment_requests_payment_request",
                table: "crypto_transactions",
                column: "payment_request_id",
                principalTable: "crypto_payment_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_loves_birds_bird_id",
                table: "loves",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_loves_users_user_id",
                table: "loves",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stories_birds_bird_id",
                table: "stories",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stories_users_author_id",
                table: "stories",
                column: "author_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_support_transactions_birds_bird_id",
                table: "support_transactions",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_support_transactions_users_supporter_id",
                table: "support_transactions",
                column: "supporter_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_support_usage_birds_bird_id",
                table: "support_usage",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_support_usage_users_reported_by",
                table: "support_usage",
                column: "reported_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bird_premium_subscriptions_birds_bird_id",
                table: "bird_premium_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_bird_premium_subscriptions_users_owner_id",
                table: "bird_premium_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_birds_users_owner_id",
                table: "birds");

            migrationBuilder.DropForeignKey(
                name: "fk_crypto_transactions_crypto_payment_requests_payment_request",
                table: "crypto_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_loves_birds_bird_id",
                table: "loves");

            migrationBuilder.DropForeignKey(
                name: "fk_loves_users_user_id",
                table: "loves");

            migrationBuilder.DropForeignKey(
                name: "fk_stories_birds_bird_id",
                table: "stories");

            migrationBuilder.DropForeignKey(
                name: "fk_stories_users_author_id",
                table: "stories");

            migrationBuilder.DropForeignKey(
                name: "fk_support_transactions_birds_bird_id",
                table: "support_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_support_transactions_users_supporter_id",
                table: "support_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_support_usage_birds_bird_id",
                table: "support_usage");

            migrationBuilder.DropForeignKey(
                name: "fk_support_usage_users_reported_by",
                table: "support_usage");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_support_usage",
                table: "support_usage");

            migrationBuilder.DropPrimaryKey(
                name: "pk_support_transactions",
                table: "support_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stories",
                table: "stories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_platform_wallets",
                table: "platform_wallets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_loves",
                table: "loves");

            migrationBuilder.DropPrimaryKey(
                name: "pk_crypto_transactions",
                table: "crypto_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_crypto_payment_requests",
                table: "crypto_payment_requests");

            migrationBuilder.DropPrimaryKey(
                name: "pk_crypto_payment_methods",
                table: "crypto_payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "pk_crypto_exchange_rates",
                table: "crypto_exchange_rates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_birds",
                table: "birds");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bird_premium_subscriptions",
                table: "bird_premium_subscriptions");

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

            migrationBuilder.RenameIndex(
                name: "ix_support_usage_reported_by",
                table: "support_usage",
                newName: "IX_support_usage_reported_by");

            migrationBuilder.RenameIndex(
                name: "ix_support_usage_bird_id",
                table: "support_usage",
                newName: "IX_support_usage_bird_id");

            migrationBuilder.RenameIndex(
                name: "ix_support_transactions_supporter_id",
                table: "support_transactions",
                newName: "IX_support_transactions_supporter_id");

            migrationBuilder.RenameIndex(
                name: "ix_support_transactions_bird_id",
                table: "support_transactions",
                newName: "IX_support_transactions_bird_id");

            migrationBuilder.RenameIndex(
                name: "ix_stories_bird_id",
                table: "stories",
                newName: "IX_stories_bird_id");

            migrationBuilder.RenameIndex(
                name: "ix_stories_author_id",
                table: "stories",
                newName: "IX_stories_author_id");

            migrationBuilder.RenameIndex(
                name: "ix_platform_wallets_currency_network_address",
                table: "platform_wallets",
                newName: "IX_platform_wallets_currency_network_address");

            migrationBuilder.RenameIndex(
                name: "ix_loves_bird_id",
                table: "loves",
                newName: "IX_loves_bird_id");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_transactions_transaction_hash",
                table: "crypto_transactions",
                newName: "IX_crypto_transactions_transaction_hash");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_transactions_payment_request_id",
                table: "crypto_transactions",
                newName: "IX_crypto_transactions_payment_request_id");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_payment_requests_transaction_hash",
                table: "crypto_payment_requests",
                newName: "IX_crypto_payment_requests_transaction_hash");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_payment_requests_status",
                table: "crypto_payment_requests",
                newName: "IX_crypto_payment_requests_status");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_payment_requests_expires_at",
                table: "crypto_payment_requests",
                newName: "IX_crypto_payment_requests_expires_at");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_payment_methods_user_id_wallet_address_currency_netw",
                table: "crypto_payment_methods",
                newName: "IX_crypto_payment_methods_user_id_wallet_address_currency_netw~");

            migrationBuilder.RenameIndex(
                name: "ix_crypto_exchange_rates_currency",
                table: "crypto_exchange_rates",
                newName: "IX_crypto_exchange_rates_currency");

            migrationBuilder.RenameIndex(
                name: "ix_birds_owner_id",
                table: "birds",
                newName: "IX_birds_owner_id");

            migrationBuilder.RenameIndex(
                name: "ix_bird_premium_subscriptions_owner_id",
                table: "bird_premium_subscriptions",
                newName: "IX_bird_premium_subscriptions_owner_id");

            migrationBuilder.RenameIndex(
                name: "ix_bird_premium_subscriptions_bird_id",
                table: "bird_premium_subscriptions",
                newName: "IX_bird_premium_subscriptions_bird_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "user_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_support_usage",
                table: "support_usage",
                column: "usage_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_support_transactions",
                table: "support_transactions",
                column: "transaction_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_stories",
                table: "stories",
                column: "story_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_platform_wallets",
                table: "platform_wallets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_loves",
                table: "loves",
                columns: new[] { "user_id", "bird_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_crypto_transactions",
                table: "crypto_transactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_crypto_payment_requests",
                table: "crypto_payment_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_crypto_payment_methods",
                table: "crypto_payment_methods",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_crypto_exchange_rates",
                table: "crypto_exchange_rates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_birds",
                table: "birds",
                column: "bird_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bird_premium_subscriptions",
                table: "bird_premium_subscriptions",
                column: "id");

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("26106c54-99bb-4f9a-81f5-9845a95a05ec"), "USDC", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2568), "coingecko", 1m },
                    { new Guid("269e39df-8fc8-4b37-9b37-5ee3491ea04e"), "BTC", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2547), "coingecko", 50000m },
                    { new Guid("2ab74aaa-79b0-48ac-b463-1c31631f8ff9"), "DOGE", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2575), "coingecko", 0.1m },
                    { new Guid("3b8b5ffc-8dde-4a0f-b2d9-f9f356c528ee"), "SOL", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2573), "coingecko", 100m },
                    { new Guid("7df322cb-48c3-445f-8380-5c52ee6a0ec4"), "USDT", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2555), "coingecko", 1m },
                    { new Guid("92860bc0-7342-4749-ac2d-0727f0264b4e"), "ETH", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2553), "coingecko", 3000m },
                    { new Guid("b3ab45ce-c2ee-4a74-8feb-70ee37d86071"), "BNB", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2570), "coingecko", 500m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[] { new Guid("549b7eb3-329e-4c25-8a0f-1774b3bad08b"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2328), "USDT", null, true, "tron", null, new DateTime(2025, 12, 10, 0, 53, 14, 206, DateTimeKind.Utc).AddTicks(2329) });

            migrationBuilder.AddForeignKey(
                name: "FK_bird_premium_subscriptions_birds_bird_id",
                table: "bird_premium_subscriptions",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bird_premium_subscriptions_users_owner_id",
                table: "bird_premium_subscriptions",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_birds_users_owner_id",
                table: "birds",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_crypto_transactions_crypto_payment_requests_payment_request~",
                table: "crypto_transactions",
                column: "payment_request_id",
                principalTable: "crypto_payment_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_loves_birds_bird_id",
                table: "loves",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_loves_users_user_id",
                table: "loves",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stories_birds_bird_id",
                table: "stories",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stories_users_author_id",
                table: "stories",
                column: "author_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_transactions_birds_bird_id",
                table: "support_transactions",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_transactions_users_supporter_id",
                table: "support_transactions",
                column: "supporter_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_usage_birds_bird_id",
                table: "support_usage",
                column: "bird_id",
                principalTable: "birds",
                principalColumn: "bird_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_usage_users_reported_by",
                table: "support_usage",
                column: "reported_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
