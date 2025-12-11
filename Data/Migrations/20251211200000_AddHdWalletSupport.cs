using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHdWalletSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add address_index column to track HD derivation path index
            migrationBuilder.AddColumn<int>(
                name: "address_index",
                table: "crypto_payment_requests",
                type: "integer",
                nullable: true);

            // Create index for efficient querying by address_index
            migrationBuilder.CreateIndex(
                name: "ix_crypto_payment_requests_address_index",
                table: "crypto_payment_requests",
                column: "address_index");

            // Create sequences for HD address index allocation per network
            // These sequences ensure atomic, collision-free address derivation
            migrationBuilder.Sql(@"
                -- Global sequence (fallback)
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Ethereum mainnet
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_ethereum
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Sepolia testnet
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_sepolia
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Tron mainnet
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_tron
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Binance Smart Chain
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_binance_smart_chain
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Polygon
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_polygon
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Bitcoin
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_bitcoin
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;

                -- Solana
                CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_solana
                    START WITH 1
                    INCREMENT BY 1
                    NO MAXVALUE
                    NO CYCLE;
            ");

            // Add comments for documentation
            migrationBuilder.Sql(@"
                COMMENT ON COLUMN crypto_payment_requests.address_index IS 'HD wallet derivation path index (BIP44). Null for non-HD addresses.';
                COMMENT ON SEQUENCE hd_address_index_seq IS 'Global HD wallet address index sequence (fallback)';
                COMMENT ON SEQUENCE hd_address_index_seq_ethereum IS 'HD wallet address index sequence for Ethereum mainnet';
                COMMENT ON SEQUENCE hd_address_index_seq_sepolia IS 'HD wallet address index sequence for Sepolia testnet';
                COMMENT ON SEQUENCE hd_address_index_seq_tron IS 'HD wallet address index sequence for Tron mainnet';
                COMMENT ON SEQUENCE hd_address_index_seq_binance_smart_chain IS 'HD wallet address index sequence for Binance Smart Chain';
                COMMENT ON SEQUENCE hd_address_index_seq_polygon IS 'HD wallet address index sequence for Polygon';
                COMMENT ON SEQUENCE hd_address_index_seq_bitcoin IS 'HD wallet address index sequence for Bitcoin';
                COMMENT ON SEQUENCE hd_address_index_seq_solana IS 'HD wallet address index sequence for Solana';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop sequences
            migrationBuilder.Sql(@"
                DROP SEQUENCE IF EXISTS hd_address_index_seq;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_ethereum;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_sepolia;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_tron;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_binance_smart_chain;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_polygon;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_bitcoin;
                DROP SEQUENCE IF EXISTS hd_address_index_seq_solana;
            ");

            // Drop index
            migrationBuilder.DropIndex(
                name: "ix_crypto_payment_requests_address_index",
                table: "crypto_payment_requests");

            // Drop column
            migrationBuilder.DropColumn(
                name: "address_index",
                table: "crypto_payment_requests");
        }
    }
}
