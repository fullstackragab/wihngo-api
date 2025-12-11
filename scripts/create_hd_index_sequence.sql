-- Create a global sequence for HD address index allocation
-- Run this in Postgres once before using HD per-payment address derivation.

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- Create per-network sequences (recommended to avoid cross-network index reuse)
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_ethereum START 1;
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_sepolia START 1;
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_tron START 1;
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_binance_smart_chain START 1;
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_polygon START 1;

-- You can add additional sequences for other networks by following the pattern:
-- CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_<network_name> START 1;

-- Note: sequence names should contain only lowercase letters, numbers and underscores.
