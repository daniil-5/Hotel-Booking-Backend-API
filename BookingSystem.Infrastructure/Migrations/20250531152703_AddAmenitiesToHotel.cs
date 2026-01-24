using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAmenitiesToHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add a temporary column with the new type
            migrationBuilder.AddColumn<string>(
                name: "amenities_jsonb",
                table: "hotels",
                type: "jsonb",
                nullable: true);

            // Copy data with explicit conversion (if the column exists)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'hotels' AND column_name = 'amenities'
                    ) THEN
                        UPDATE hotels SET amenities_jsonb = 
                            CASE 
                                WHEN amenities IS NULL THEN '[]'::jsonb
                                ELSE to_jsonb(amenities)
                            END;
                    END IF;
                END $$;
            ");

            // Drop the old column
            migrationBuilder.DropColumn(
                name: "amenities",
                table: "hotels");

            // Rename the new column to the original name
            migrationBuilder.RenameColumn(
                name: "amenities_jsonb",
                table: "hotels",
                newName: "amenities");

            // Add the same for RoomType entity
            migrationBuilder.AddColumn<string>(
                name: "amenities",
                table: "room_types",
                type: "jsonb",
                nullable: true,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // For the down migration, convert back to text array
            migrationBuilder.AlterColumn<string[]>(
                name: "amenities",
                table: "hotels",
                type: "text[]",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            // Remove the column from RoomType
            migrationBuilder.DropColumn(
                name: "amenities",
                table: "room_types");
        }
    }
}
