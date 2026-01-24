using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingsAddRoomTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add room_type_id column if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_type_id'
                    ) THEN
                        ALTER TABLE bookings ADD COLUMN room_type_id integer;
                    END IF;
                END $$;
            ");

            // Step 2: Migrate data from room_id to room_type_id
            migrationBuilder.Sql(@"
                UPDATE bookings b
                SET room_type_id = r.room_type_id
                FROM rooms r
                WHERE b.room_id = r.id AND b.room_type_id IS NULL;
            ");

            // Step 3: For any remaining NULL room_type_id, set to a default value
            migrationBuilder.Sql(@"
                UPDATE bookings 
                SET room_type_id = (SELECT id FROM room_types ORDER BY id LIMIT 1)
                WHERE room_type_id IS NULL;
            ");

            // Step 4: Replace values > 35 with random values 1-35
            migrationBuilder.Sql(@"
                UPDATE bookings
                SET room_type_id = 1 + floor(random() * 35)::integer
                WHERE room_type_id > 35;
            ");

            // Step 5: Make room_type_id NOT NULL
            migrationBuilder.Sql(@"
                ALTER TABLE bookings ALTER COLUMN room_type_id SET NOT NULL;
            ");

            // Step 6: Create index for room_type_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'IX_bookings_room_type_id'
                    ) THEN
                        CREATE INDEX ""IX_bookings_room_type_id"" ON bookings (room_type_id);
                    END IF;
                END $$;
            ");

            // Step 7: Add foreign key constraint for room_type_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'f_k_bookings_room_types_room_type_id'
                    ) THEN
                        ALTER TABLE bookings 
                        ADD CONSTRAINT f_k_bookings_room_types_room_type_id 
                        FOREIGN KEY (room_type_id) 
                        REFERENCES room_types (id) 
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");

            // Step 8: Drop the foreign key constraint for room_id if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'f_k_bookings_rooms_room_id'
                    ) THEN
                        ALTER TABLE bookings DROP CONSTRAINT f_k_bookings_rooms_room_id;
                    END IF;
                END $$;
            ");

            // Step 9: Drop the index for room_id if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'IX_bookings_room_id'
                    ) THEN
                        DROP INDEX ""IX_bookings_room_id"";
                    END IF;
                END $$;
            ");

            // Step 10: Drop the room_id column
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_id'
                    ) THEN
                        ALTER TABLE bookings DROP COLUMN room_id;
                    END IF;
                END $$;
            ");

            // Step 11: Do the same for room_pricings table if needed
            migrationBuilder.Sql(@"
                -- Add room_type_id to room_pricings if needed
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'room_pricings' AND column_name = 'room_type_id'
                    ) THEN
                        ALTER TABLE room_pricings ADD COLUMN room_type_id integer;
                        
                        -- Migrate data from room_id to room_type_id
                        UPDATE room_pricings p
                        SET room_type_id = r.room_type_id
                        FROM rooms r
                        WHERE p.room_id = r.id;
                        
                        -- Fix values > 35
                        UPDATE room_pricings
                        SET room_type_id = 1 + floor(random() * 35)::integer
                        WHERE room_type_id > 35;
                        
                        -- Set default for NULL values
                        UPDATE room_pricings 
                        SET room_type_id = (SELECT id FROM room_types ORDER BY id LIMIT 1)
                        WHERE room_type_id IS NULL;
                        
                        -- Make NOT NULL
                        ALTER TABLE room_pricings ALTER COLUMN room_type_id SET NOT NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add room_id column back
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_id'
                    ) THEN
                        ALTER TABLE bookings ADD COLUMN room_id integer;
                    END IF;
                END $$;
            ");

            // Step 2: Create index for room_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'IX_bookings_room_id'
                    ) THEN
                        CREATE INDEX ""IX_bookings_room_id"" ON bookings (room_id);
                    END IF;
                END $$;
            ");

            // Step 3: Drop the foreign key constraint for room_type_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'f_k_bookings_room_types_room_type_id'
                    ) THEN
                        ALTER TABLE bookings DROP CONSTRAINT f_k_bookings_room_types_room_type_id;
                    END IF;
                END $$;
            ");

            // Step 4: Drop the index for room_type_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'IX_bookings_room_type_id'
                    ) THEN
                        DROP INDEX ""IX_bookings_room_type_id"";
                    END IF;
                END $$;
            ");

            // Step 5: Drop the room_type_id column
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_type_id'
                    ) THEN
                        ALTER TABLE bookings DROP COLUMN room_type_id;
                    END IF;
                END $$;
            ");
        }
    }
}
