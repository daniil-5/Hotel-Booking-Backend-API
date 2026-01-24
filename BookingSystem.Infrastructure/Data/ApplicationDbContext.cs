using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected AppDbContext() { }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<HotelPhoto> HotelPhotos { get; set; }
        public DbSet<RoomPricing> RoomPricings { get; set; }
        public DbSet<Amenity> Amenities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure table names
            modelBuilder.Entity<Hotel>().ToTable("hotels");
            modelBuilder.Entity<RoomType>().ToTable("room_types");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<HotelPhoto>().ToTable("hotel_photos");
            modelBuilder.Entity<RoomPricing>().ToTable("room_pricings");
            modelBuilder.Entity<Amenity>().ToTable("amenities");

            // Hotel - Amenity (Many-to-Many)
            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Amenities)
                .WithMany(a => a.Hotels)
                .UsingEntity(j => j.ToTable("hotel_amenities"));

            // Hotel - RoomType relationship
            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.RoomTypes)
                .WithOne(rt => rt.Hotel)
                .HasForeignKey(rt => rt.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            // RoomType - RoomPricing relationship
            modelBuilder.Entity<RoomType>()
                .HasMany(rt => rt.Pricing)
                .WithOne(rp => rp.RoomType)
                .HasForeignKey(rp => rp.RoomTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - Booking relationship
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hotel - Booking relationship
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Hotel)
                .WithMany(h => h.Bookings)
                .HasForeignKey(b => b.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // RoomType - Booking relationship
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.RoomType)
                .WithMany(rt => rt.Bookings)
                .HasForeignKey(b => b.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RoomPricing>()
                .Property(rp => rp.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RoomType>()
                .Property(rt => rt.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RoomType>()
                .Property(rt => rt.Area)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Hotel>()
                .Property(h => h.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Hotel>()
                .Property(h => h.Rating)
                .HasColumnType("decimal(2,1)");

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName().ToSnakeCase());

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.Name.ToSnakeCase());
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().ToSnakeCase());
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    foreignKey.SetConstraintName(foreignKey.GetConstraintName().ToSnakeCase());
                }
            }
        }
    }

    public static class StringExtensions
    {
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return string.Concat(input.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + c.ToString().ToLower()
                    : c.ToString().ToLower()));
        }
    }
}
