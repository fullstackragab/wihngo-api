using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBirdPremiumColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("59b7b668-b1ba-40b5-be17-4bbb6d69cd72"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("5a2cca79-293b-4863-a1e3-e8f3be525bfe"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("78211331-477d-4399-9bb2-e63c2e939852"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("7bac845b-a02a-4d7d-83ea-0727898e73d4"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("b70e13d4-4c36-4848-82b7-250952e56422"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("bfb4bc3e-b936-41c8-a7d8-120944b066c9"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("dd0ee405-6005-4a5c-ad8b-e243c375be00"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("1a0b9e7d-73b7-436d-8b46-780a3b5b35f8"));

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
