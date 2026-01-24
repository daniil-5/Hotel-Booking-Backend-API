using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class CorrectBookingEntityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First check if the index exists and drop it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'ix_bookings_room_id'
                    ) THEN
                        DROP INDEX ""ix_bookings_room_id"";
                    END IF;
                END $$;
            ");

            // Remove the foreign key constraint if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint 
                        WHERE conname = 'f_k_bookings_rooms_room_id'
                    ) THEN
                        ALTER TABLE bookings DROP CONSTRAINT f_k_bookings_rooms_room_id;
                    END IF;
                END $$;
            ");

            // Finally drop the room_id column if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_id'
                    ) THEN
                        ALTER TABLE bookings DROP COLUMN room_id;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add room_id column back
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'bookings' AND column_name = 'room_id'
                    ) THEN
                        ALTER TABLE bookings ADD COLUMN room_id integer;
                    END IF;
                END $$;
            ");

            // Create index for room_id
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE tablename = 'bookings' AND indexname = 'ix_bookings_room_id'
                    ) THEN
                        CREATE INDEX ""ix_bookings_room_id"" ON bookings (room_id);
                    END IF;
                END $$;
            ");
        }
    }
}
