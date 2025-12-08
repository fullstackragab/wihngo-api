using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wihngo.Data.Migrations
{
    public partial class AlterLovesTableOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with guards so this migration is small and safe to run against
            // an existing schema that may already contain other tables.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- add created_at column if missing
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'loves' AND column_name = 'created_at'
    ) THEN
        ALTER TABLE loves ADD COLUMN created_at timestamptz NOT NULL DEFAULT now();
    END IF;

    -- remove duplicate rows if any (keep one) to allow primary key creation
    -- this will not touch rows that are already unique
    IF EXISTS (
        SELECT 1 FROM (
            SELECT user_id, bird_id, COUNT(*) as cnt
            FROM loves
            GROUP BY user_id, bird_id
            HAVING COUNT(*) > 1
        ) t
    ) THEN
        WITH ranked AS (
            SELECT ctid, user_id, bird_id,
                   ROW_NUMBER() OVER (PARTITION BY user_id, bird_id ORDER BY created_at NULLS LAST) AS rn
            FROM loves
        )
        DELETE FROM loves
        WHERE ctid IN (SELECT ctid FROM ranked WHERE rn > 1);
    END IF;

    -- primary key creation intentionally omitted here to avoid duplicate-primary-key errors

    -- add foreign key to users if missing (check any FK from loves -> users)
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        JOIN pg_class r ON c.confrelid = r.oid
        WHERE t.relname = 'loves' AND r.relname = 'users' AND c.contype = 'f'
    ) THEN
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users') THEN
            ALTER TABLE loves ADD CONSTRAINT fk_loves_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;
        END IF;
    END IF;

    -- add foreign key to birds if missing
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        JOIN pg_class r ON c.confrelid = r.oid
        WHERE t.relname = 'loves' AND r.relname = 'birds' AND c.contype = 'f'
    ) THEN
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'birds') THEN
            ALTER TABLE loves ADD CONSTRAINT fk_loves_bird FOREIGN KEY (bird_id) REFERENCES birds(bird_id) ON DELETE CASCADE;
        END IF;
    END IF;

    -- add index on bird_id if missing
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE tablename = 'loves' AND indexdef LIKE '%(bird_id)%'
    ) THEN
        CREATE INDEX ix_loves_bird_id ON loves(bird_id);
    END IF;
END$$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- drop index if exists
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'loves' AND indexdef LIKE '%(bird_id)%') THEN
        DROP INDEX IF EXISTS ix_loves_bird_id;
    END IF;

    -- drop foreign keys if exist
    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        JOIN pg_class r ON c.confrelid = r.oid
        WHERE t.relname = 'loves' AND r.relname = 'birds' AND c.contype = 'f'
    ) THEN
        ALTER TABLE loves DROP CONSTRAINT IF EXISTS fk_loves_bird;
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        JOIN pg_class r ON c.confrelid = r.oid
        WHERE t.relname = 'loves' AND r.relname = 'users' AND c.contype = 'f'
    ) THEN
        ALTER TABLE loves DROP CONSTRAINT IF EXISTS fk_loves_user;
    END IF;

    -- do not drop primary key or table to avoid data loss
END$$;
");
        }
    }
}
