using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly Random _random = new Random();

        private readonly DateTime _baseDate = DateTime.UtcNow;

        private readonly string[] _amenityNames = {
            "Free Wi-Fi", "Swimming Pool", "Gym", "Spa", "Parking", "Restaurant",
            "Bar", "24/7 Front Desk", "Room Service", "Airport Shuttle",
            "Air Conditioning", "Pet Friendly", "Conference Room", "Sea View",
            "Breakfast Included", "Smart TV", "Mini Bar", "Coffee Maker"
        };

        private readonly string[] _hotelNames = {
            "Grand Plaza Hotel", "Seaside Resort", "Mountain View Lodge", "City Center Inn",
            "Golden Gate Suites", "Riverside Retreat", "Royal Palace Hotel", "Sunset Bay Resort",
            "The Metropolitan", "Harbor View Hotel", "Ocean Paradise", "Forest Hills Lodge"
        };

        private readonly string[] _locations = {
            "New York, NY", "Miami, FL", "Denver, CO", "San Francisco, CA", "Chicago, IL",
            "Boston, MA", "Seattle, WA", "Las Vegas, NV", "Austin, TX", "New Orleans, LA"
        };

        private readonly string[] _roomTypeNames = {
            "Standard", "Deluxe", "Suite", "Family Room", "Penthouse", "Executive",
            "Junior Suite", "Studio", "Connecting Room", "Accessible Room"
        };

        private readonly string[] _photoUrls = {
            "https://images.unsplash.com/photo-1566073771259-6a8506099945",
            "https://images.unsplash.com/photo-1564501049412-61c2a3083791",
            "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa",
            "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6"
        };

        public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedUsersAsync();
                await SeedAmenitiesAsync();
                await SeedHotelsAsync();
                await SeedHotelPhotosAsync();
                await SeedRoomTypesAsync();
                await SeedRoomPricingsAsync();
                await SeedBookingsAsync();

                _logger.LogInformation("Database seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR while seeding the database");
                throw;
            }
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync()) return;

            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@bs.com",
                    Role = (int)UserRole.Admin,
                    PasswordHash = "hash",
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "+1234567890"
                },
                new User
                {
                    Username = "manager",
                    Email = "manager@bs.com",
                    Role = (int)UserRole.Manager,
                    PasswordHash = "hash",
                    FirstName = "Manager",
                    LastName = "User",
                    PhoneNumber = "+1234567891"
                },
                new User
                {
                    Username = "guest",
                    Email = "guest@bs.com",
                    Role = (int)UserRole.Guest,
                    PasswordHash = "hash",
                    FirstName = "Guest",
                    LastName = "User",
                    PhoneNumber = "+1234567892"
                }
            };

            for (int i = 1; i <= 5; i++)
            {
                users.Add(new User
                {
                    Username = $"user{i}",
                    Email = $"user{i}@example.com",
                    FirstName = "Test",
                    LastName = $"User{i}",
                    PasswordHash = "hash",
                    Role = (int)UserRole.Guest,
                    PhoneNumber = $"+100000000{i}"
                });
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded Users");
        }

        private async Task SeedAmenitiesAsync()
        {
            if (await _context.Amenities.AnyAsync()) return;

            var amenities = _amenityNames.Select(name => new Amenity
            {
                Name = name,
                Description = $"Enjoy our {name.ToLower()}",
                CreatedAt = _baseDate
            }).ToList();

            await _context.Amenities.AddRangeAsync(amenities);
            await _context.SaveChangesAsync();
        }

        private async Task SeedHotelsAsync()
        {
            if (await _context.Hotels.AnyAsync()) return;

            var amenities = await _context.Amenities.ToListAsync();
            var hotels = new List<Hotel>();

            for (int i = 0; i < _hotelNames.Length; i++)
            {
                var hotel = new Hotel
                {
                    Name = _hotelNames[i],
                    Description = $"Experience luxury at {_hotelNames[i]}. Located in the heart of {_locations[i % _locations.Length]}.",
                    Location = _locations[i % _locations.Length],
                    Rating = (decimal)(3.5 + _random.NextDouble() * 1.5),
                    BasePrice = 100 + _random.Next(200),
                    CreatedAt = _baseDate
                };

                var randomAmenities = amenities.OrderBy(x => _random.Next()).Take(_random.Next(5, 12)).ToList();
                foreach (var am in randomAmenities)
                {
                    hotel.Amenities.Add(am);
                }

                hotels.Add(hotel);
            }

            await _context.Hotels.AddRangeAsync(hotels);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded Hotels");
        }

        private async Task SeedHotelPhotosAsync()
        {
            if (await _context.HotelPhotos.AnyAsync()) return;
            var hotels = await _context.Hotels.ToListAsync();
            var photos = new List<HotelPhoto>();

            foreach (var hotel in hotels)
            {
                for (int i = 0; i < 3; i++)
                {
                    photos.Add(new HotelPhoto
                    {
                        HotelId = hotel.Id,
                        Url = _photoUrls[i % _photoUrls.Length],
                        PublicId = Guid.NewGuid().ToString(),
                        Description = i == 0 ? "Main View" : "Interior",
                        IsMain = i == 0,
                        CreatedAt = _baseDate
                    });
                }
            }
            await _context.HotelPhotos.AddRangeAsync(photos);
            await _context.SaveChangesAsync();
        }

        private async Task SeedRoomTypesAsync()
        {
            if (await _context.RoomTypes.AnyAsync()) return;

            var hotels = await _context.Hotels.ToListAsync();
            var roomTypes = new List<RoomType>();

            foreach (var hotel in hotels)
            {
                int typesToAdd = _random.Next(2, 5);
                for (int i = 0; i < typesToAdd; i++)
                {
                    var typeName = _roomTypeNames[i % _roomTypeNames.Length];
                    int beds = typeName.Contains("Family") ? _random.Next(3, 5) : _random.Next(1, 3);

                    roomTypes.Add(new RoomType
                    {
                        Name = typeName,
                        Description = $"Comfortable {typeName} with nice view",
                        BasePrice = hotel.BasePrice + _random.Next(50, 300),
                        Capacity = typeName.Contains("Family") ? 4 : 2,
                        Area = 20 + _random.Next(50),
                        Floor = _random.Next(1, 10),
                        Count = _random.Next(3, 10),
                        HotelId = hotel.Id,
                        CreatedAt = _baseDate,
                        BedCount = beds
                    });
                }
            }

            await _context.RoomTypes.AddRangeAsync(roomTypes);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded RoomTypes");
        }

        private async Task SeedRoomPricingsAsync()
        {
            if (await _context.RoomPricings.AnyAsync()) return;

            var roomTypes = await _context.RoomTypes.ToListAsync();
            var pricings = new List<RoomPricing>();

            foreach (var rt in roomTypes)
            {
                for (int i = 0; i < 30; i++)
                {
                    var date = _baseDate.Date.AddDays(i);
                    var isWeekend = date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;
                    var multiplier = isWeekend ? 1.2m : 1.0m;

                    pricings.Add(new RoomPricing
                    {
                        RoomTypeId = rt.Id,
                        Date = date,
                        Price = rt.BasePrice * multiplier,
                        CreatedAt = _baseDate
                    });
                }
            }
            await _context.RoomPricings.AddRangeAsync(pricings);
            await _context.SaveChangesAsync();
        }

        private async Task SeedBookingsAsync()
        {
            if (await _context.Bookings.AnyAsync()) return;

            var users = await _context.Users.Where(u => u.Role == (int)UserRole.Guest).ToListAsync();
            var roomTypes = await _context.RoomTypes.Include(rt => rt.Hotel).ToListAsync();

            if (!users.Any() || !roomTypes.Any()) return;

            var bookings = new List<Booking>();

            for (int i = 0; i < 15; i++)
            {
                var rt = roomTypes[_random.Next(roomTypes.Count)];
                var user = users[_random.Next(users.Count)];

                var checkIn = _baseDate.AddDays(_random.Next(1, 20));
                var nights = _random.Next(1, 5);
                var checkOut = checkIn.AddDays(nights);

                bookings.Add(new Booking
                {
                    UserId = user.Id,
                    RoomTypeId = rt.Id,
                    HotelId = rt.HotelId,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    GuestCount = Math.Min(2, rt.Capacity),
                    TotalPrice = rt.BasePrice * nights,
                    Status = (int)BookingStatus.Confirmed,
                    CreatedAt = _baseDate,
                    TrackingId = Guid.NewGuid()
                });
            }

            await _context.Bookings.AddRangeAsync(bookings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded Bookings");
        }
    }
}
