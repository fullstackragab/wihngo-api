using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "address_index",
                table: "crypto_payment_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blockchain_cursors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cursor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_processed_value = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blockchain_cursors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bird_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_fiat = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    fiat_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount_fiat_at_settlement = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    settlement_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preferred_payment_methods = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    issued_pdf_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    receipt_notes = table.Column<string>(type: "text", nullable: true),
                    is_tax_deductible = table.Column<bool>(type: "boolean", nullable: false),
                    solana_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    base_payment_data = table.Column<string>(type: "jsonb", nullable: true),
                    pay_pal_order_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "onchain_deposits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    token_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    address_or_account = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tx_hash_or_sig = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    block_number_or_slot = table.Column<long>(type: "bigint", nullable: true),
                    op_index_or_log_index = table.Column<int>(type: "integer", nullable: true),
                    from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    raw_amount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    decimals = table.Column<int>(type: "integer", nullable: false),
                    amount_decimal = table.Column<decimal>(type: "numeric(20,10)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmations = table.Column<int>(type: "integer", nullable: false),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    credited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    memo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onchain_deposits", x => x.id);
                    table.ForeignKey(
                        name: "fk_onchain_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    previous_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    new_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    actor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supported_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    chain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mint_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    merchant_receiving_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    decimals = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tolerance_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supported_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "token_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    chain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    issuer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    decimals = table.Column<int>(type: "integer", nullable: false),
                    required_confirmations = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    derivation_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_received",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_received", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payer_identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tx_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_tx_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    token = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    chain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    amount_crypto = table.Column<decimal>(type: "numeric(20,10)", nullable: true),
                    fiat_value_at_payment = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    block_slot = table.Column<long>(type: "bigint", nullable: true),
                    confirmations = table.Column<int>(type: "integer", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refund_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    refund_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    refund_tx_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_refund_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refund_receipt_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refund_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_refund_requests_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refund_requests_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "crypto_exchange_rates",
                columns: new[] { "id", "currency", "last_updated", "source", "usd_rate" },
                values: new object[,]
                {
                    { new Guid("0a9487d1-0e50-4059-bc6a-47169729426f"), "BTC", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(44), "coingecko", 50000m },
                    { new Guid("3221be40-f85a-44e0-9fd3-7ecacab3138d"), "DOGE", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(68), "coingecko", 0.1m },
                    { new Guid("78f3e490-cadc-4346-abb0-f539b1eafd99"), "BNB", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(62), "coingecko", 500m },
                    { new Guid("b91c6e2d-5f8e-4c35-b383-9a83c6ced14b"), "ETH", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(52), "coingecko", 3000m },
                    { new Guid("c4dd9e0f-ff56-4edf-a7c4-c870b45fe17b"), "USDC", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(59), "coingecko", 1m },
                    { new Guid("e1269d7e-a515-40a0-a050-5f66a6a3a5ef"), "SOL", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(66), "coingecko", 100m },
                    { new Guid("e36fec8c-4d1d-4c8e-bb6b-3623adf4c5cc"), "USDT", new DateTime(2025, 12, 12, 17, 9, 6, 892, DateTimeKind.Utc).AddTicks(55), "coingecko", 1m }
                });

            migrationBuilder.InsertData(
                table: "platform_wallets",
                columns: new[] { "id", "address", "created_at", "currency", "derivation_path", "is_active", "network", "private_key_encrypted", "updated_at" },
                values: new object[,]
                {
                    { new Guid("3f9986dc-8698-4d8f-9996-d419cd82c1b5"), "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9527), "ETH", null, true, "sepolia", null, new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9527) },
                    { new Guid("478620ad-94ac-4772-b0e6-d36a39d62bbb"), "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9505), "USDT", null, true, "tron", null, new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9505) },
                    { new Guid("95a85785-507e-48ec-8c3e-9202628fa244"), "0x83675000ac9915614afff618906421a2baea0020", new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9523), "USDT", null, true, "binance-smart-chain", null, new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9524) },
                    { new Guid("deb4a6dd-50b6-49a8-bcd0-5d020f67ab17"), "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9519), "USDT", null, true, "ethereum", null, new DateTime(2025, 12, 12, 17, 9, 6, 891, DateTimeKind.Utc).AddTicks(9520) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_blockchain_cursors_chain_cursor_type",
                table: "blockchain_cursors",
                columns: new[] { "chain", "cursor_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_expires_at",
                table: "invoices",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true,
                filter: "invoice_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_state",
                table: "invoices",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_user_id",
                table: "invoices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_onchain_deposits_chain_address_or_account",
                table: "onchain_deposits",
                columns: new[] { "chain", "address_or_account" });

            migrationBuilder.CreateIndex(
                name: "ix_onchain_deposits_detected_at",
                table: "onchain_deposits",
                column: "detected_at");

            migrationBuilder.CreateIndex(
                name: "ix_onchain_deposits_status",
                table: "onchain_deposits",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_onchain_deposits_tx_hash_or_sig",
                table: "onchain_deposits",
                column: "tx_hash_or_sig",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onchain_deposits_user_id",
                table: "onchain_deposits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_events_created_at",
                table: "payment_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_payment_events_invoice_id",
                table: "payment_events",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_events_payment_id",
                table: "payment_events",
                column: "payment_id",
                filter: "payment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_id",
                table: "payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_provider_tx_id",
                table: "payments",
                column: "provider_tx_id",
                filter: "provider_tx_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_tx_hash",
                table: "payments",
                column: "tx_hash",
                unique: true,
                filter: "tx_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refund_requests_invoice_id",
                table: "refund_requests",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_refund_requests_payment_id",
                table: "refund_requests",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_refund_requests_state",
                table: "refund_requests",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_supported_tokens_token_symbol_chain",
                table: "supported_tokens",
                columns: new[] { "token_symbol", "chain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_token_configurations_is_active",
                table: "token_configurations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_token_configurations_token_chain",
                table: "token_configurations",
                columns: new[] { "token", "chain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_received_processed",
                table: "webhook_received",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_received_provider_provider_event_id",
                table: "webhook_received",
                columns: new[] { "provider", "provider_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "blockchain_cursors");

            migrationBuilder.DropTable(
                name: "onchain_deposits");

            migrationBuilder.DropTable(
                name: "payment_events");

            migrationBuilder.DropTable(
                name: "refund_requests");

            migrationBuilder.DropTable(
                name: "supported_tokens");

            migrationBuilder.DropTable(
                name: "token_configurations");

            migrationBuilder.DropTable(
                name: "webhook_received");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("0a9487d1-0e50-4059-bc6a-47169729426f"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("3221be40-f85a-44e0-9fd3-7ecacab3138d"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("78f3e490-cadc-4346-abb0-f539b1eafd99"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("b91c6e2d-5f8e-4c35-b383-9a83c6ced14b"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("c4dd9e0f-ff56-4edf-a7c4-c870b45fe17b"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("e1269d7e-a515-40a0-a050-5f66a6a3a5ef"));

            migrationBuilder.DeleteData(
                table: "crypto_exchange_rates",
                keyColumn: "id",
                keyValue: new Guid("e36fec8c-4d1d-4c8e-bb6b-3623adf4c5cc"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("3f9986dc-8698-4d8f-9996-d419cd82c1b5"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("478620ad-94ac-4772-b0e6-d36a39d62bbb"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("95a85785-507e-48ec-8c3e-9202628fa244"));

            migrationBuilder.DeleteData(
                table: "platform_wallets",
                keyColumn: "id",
                keyValue: new Guid("deb4a6dd-50b6-49a8-bcd0-5d020f67ab17"));

            migrationBuilder.DropColumn(
                name: "address_index",
                table: "crypto_payment_requests");

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
    }
}
