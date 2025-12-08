using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wihngo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoveCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make this migration idempotent and safe for databases that already have other tables.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- create loves table if missing with composite PK and FKs
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'loves') THEN
        CREATE TABLE loves (
            user_id uuid NOT NULL,
            bird_id uuid NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now()
        );
    END IF;

    -- add primary key if missing (check by contype 'p' for primary key on table)
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        WHERE t.relname = 'loves' AND c.contype = 'p'
    ) THEN
        ALTER TABLE loves ADD CONSTRAINT pk_loves PRIMARY KEY (user_id, bird_id);
    END IF;

    -- add foreign key to users if missing (check for any FK referencing users)
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

    -- add foreign key to birds if missing (check for any FK referencing birds)
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

    -- add index on bird_id if missing (check indexdef)
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE tablename = 'loves' AND indexdef LIKE '%(bird_id)%'
    ) THEN
        CREATE INDEX ix_loves_bird_id ON loves(bird_id);
    END IF;
END$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'loves' AND indexdef LIKE '%(bird_id)%') THEN
        DROP INDEX IF EXISTS ix_loves_bird_id;
    END IF;

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

    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON c.conrelid = t.oid
        WHERE t.relname = 'loves' AND c.contype = 'p'
    ) THEN
        ALTER TABLE loves DROP CONSTRAINT IF EXISTS pk_loves;
    END IF;

    -- do not drop table to avoid data loss; leave table if it existed
END$$;
");
        }
    }
}
