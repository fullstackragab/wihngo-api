using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSepoliaWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("147e7687-96b2-41d8-86a2-3d0cca37285c"), "SOL", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8467), "coingecko", 100m },
                    { new Guid("1b6fcc50-4f8a-4ef5-b006-8797f97159bf"), "BTC", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8444), "coingecko", 50000m },
                    { new Guid("4b85ae4b-d46e-4537-bee5-c5a29ba1d4a5"), "DOGE", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8470), "coingecko", 0.1m },
                    { new Guid("911e029b-b0c8-4a1e-929d-8b9e01ef8970"), "ETH", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8450), "coingecko", 3000m },
                    { new Guid("97ba79ed-3bc3-46b7-9469-5e8e5baa4877"), "USDT", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8454), "coingecko", 1m },
                    { new Guid("beb1a94c-12ec-4713-b54f-0289d1b8f55a"), "BNB", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8464), "coingecko", 500m },
                    { new Guid("c5809069-c3d4-4829-9732-52ab33e527db"), "USDC", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8458), "coingecko", 1m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[,]
                {
                    { new Guid("2e1d42b0-1d81-48bf-80b8-9d70f3eb0ad9"), "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8269), "ETH", null, true, "sepolia", null, new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8270) },
                    { new Guid("36118036-9158-4a66-82cd-81e606e8453e"), "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8261), "USDT", null, true, "ethereum", null, new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8262) },
                    { new Guid("4855b754-106a-4075-ba18-a0db74007bdb"), "0x83675000ac9915614afff618906421a2baea0020", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8265), "USDT", null, true, "binance-smart-chain", null, new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8266) },
                    { new Guid("88e4ce24-e1ab-4755-98e7-a161322fa40c"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8255), "USDT", null, true, "tron", null, new DateTime(2025, 12, 11, 16, 26, 7, 480, DateTimeKind.Utc).AddTicks(8256) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("147e7687-96b2-41d8-86a2-3d0cca37285c"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("1b6fcc50-4f8a-4ef5-b006-8797f97159bf"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("4b85ae4b-d46e-4537-bee5-c5a29ba1d4a5"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("911e029b-b0c8-4a1e-929d-8b9e01ef8970"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("97ba79ed-3bc3-46b7-9469-5e8e5baa4877"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("beb1a94c-12ec-4713-b54f-0289d1b8f55a"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("c5809069-c3d4-4829-9732-52ab33e527db"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("2e1d42b0-1d81-48bf-80b8-9d70f3eb0ad9"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("36118036-9158-4a66-82cd-81e606e8453e"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("4855b754-106a-4075-ba18-a0db74007bdb"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("88e4ce24-e1ab-4755-98e7-a161322fa40c"));

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
        }
    }
}
