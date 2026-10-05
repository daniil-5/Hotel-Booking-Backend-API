//
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using BookingSystem.Domain.Entities;
// using BookingSystem.Domain.Enums;
//
// namespace BookingSystem.Infrastructure.Data
// {
//     public class DatabaseSeeder
//     {
//         private readonly AppDbContext _context;
//         private readonly ILogger<DatabaseSeeder> _logger;
//         private readonly DateTime _currentDate = new DateTime(2025, 5, 3, 18, 2, 52, DateTimeKind.Utc);
//         private readonly Random _random = new Random();
//         
//         // Sample data
//         private readonly string[] _hotelNames = {
//             "Grand Plaza Hotel", "Seaside Resort", "Mountain View Lodge", "City Center Inn", 
//             "Golden Gate Suites", "Riverside Retreat", "Royal Palace Hotel", "Sunset Bay Resort",
//             "The Metropolitan", "Harbor View Hotel", "Ocean Paradise", "Forest Hills Lodge"
//         };
//         
//         private readonly string[] _locations = {
//             "New York, NY", "Miami, FL", "Denver, CO", "San Francisco, CA", "Chicago, IL",
//             "Boston, MA", "Seattle, WA", "Las Vegas, NV", "Austin, TX", "New Orleans, LA"
//         };
//         
//         private readonly string[] _roomTypeNames = {
//             "Standard", "Deluxe", "Suite", "Family Room", "Penthouse", "Executive", 
//             "Junior Suite", "Studio", "Connecting Room", "Accessible Room"
//         };
//         
//         private readonly string[] _firstNames = {
//             "John", "Jane", "Michael", "Emma", "William", "Olivia", "James", "Sophia",
//             "Robert", "Ava", "David", "Isabella", "Joseph", "Mia", "Thomas", "Charlotte"
//         };
//         
//         private readonly string[] _lastNames = {
//             "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
//             "Rodriguez", "Martinez", "Wilson", "Anderson", "Taylor", "Thomas", "Moore", "Jackson"
//         };
//         
//         private readonly string[] _photoDescriptions = {
//             "Exterior View", "Lobby", "Main Entrance", "Swimming Pool", "Restaurant",
//             "Gym", "Spa", "Conference Room", "Garden", "Ocean View", "Mountain View", "Room Interior"
//         };
//         
//         private readonly string[] _photoUrls = {
//             "https://images.unsplash.com/photo-1566073771259-6a8506099945",
//             "https://images.unsplash.com/photo-1564501049412-61c2a3083791",
//             "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa",
//             "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6",
//             "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4",
//             "https://images.unsplash.com/photo-1522798514-97ceb8c4f1c8",
//             "https://images.unsplash.com/photo-1445019980597-93fa8acb246c",
//             "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb",
//             "https://images.unsplash.com/photo-1571896349842-33c89424de2d",
//             "https://images.unsplash.com/photo-1629140727571-9b5c6f6267b4",
//             "https://images.unsplash.com/photo-1590490360182-c33d57733427",
//             "https://images.unsplash.com/photo-1582719508461-905c673771fd"
//         };
//
//         public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
//         {
//             _context = context;
//             _logger = logger;
//         }
//
//         public async Task SeedAsync()
//         {
//             try
//             {
//                 // Apply pending migrations
//                 await _context.Database.MigrateAsync();
//                 
//                 // Seed data in order of dependencies
//                 await SeedUsersAsync();
//                 await SeedHotelsAsync();
//                 await SeedHotelPhotosAsync();
//                 await SeedRoomTypesAsync();
//                 await SeedRoomPricingsAsync();
//                 await SeedBookingsAsync();
//                 
//                 _logger.LogInformation("Database seeded successfully at {time}", _currentDate);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An error occurred while seeding the database at {time}", _currentDate);
//                 throw;
//             }
//         }
//
//          public async Task SeedUsersOnlyAsync()
//         {
//             try
//             {
//                 await SeedUsersAsync(true);
//                 _logger.LogInformation("Users seeded successfully at {time}", _currentDate);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An error occurred while seeding users at {time}", _currentDate);
//                 throw;
//             }
//         }
//
//         private async Task SeedUsersAsync(bool forceUserSeed = false)
//         {
//             // Check if users exist and if we should skip
//             bool usersExist = await _context.Users.AnyAsync();
//             
//             if (usersExist)
//             {
//                 _logger.LogInformation("Users table already has data");
//                 
//                 if (!forceUserSeed)
//                 {
//                     _logger.LogInformation("Skipping user seeding. Use forceUserSeed=true to override.");
//                     return;
//                 }
//                 
//                 _logger.LogInformation("Force seeding users despite existing data");
//             }
//
//             try
//             {
//                 var users = new List<User>
//                 {
//                     new User
//                     {
//                         Username = "admin",
//                         Email = "admin@bookingsystem.com",
//                         FirstName = "Admin",
//                         LastName = "User",
//                         PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
//                         PhoneNumber = "+1234567890",
//                         Role = (int)UserRole.Admin
//                     },
//                     new User
//                     {
//                         Username = "manager",
//                         Email = "manager@bookingsystem.com",
//                         FirstName = "Manager",
//                         LastName = "User",
//                         PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
//                         PhoneNumber = "+1987654321",
//                         Role = (int)UserRole.Manager
//                     }
//                 };
//
//                 // Add 8 regular users
//                 for (int i = 1; i <= 8; i++)
//                 {
//                     var firstName = _firstNames[_random.Next(_firstNames.Length)];
//                     var lastName = _lastNames[_random.Next(_lastNames.Length)];
//                     
//                     users.Add(new User
//                     {
//                         Username = $"user{i}",
//                         Email = $"user{i}@example.com",
//                         FirstName = firstName,
//                         LastName = lastName,
//                         PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
//                         PhoneNumber = $"+1{_random.Next(100, 999)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}",
//                         Role = (int)UserRole.Guest
//                     });
//                 }
//
//                 // Check if we need to clear existing users first
//                 if (usersExist && forceUserSeed)
//                 {
//                     _logger.LogInformation("Removing existing users before re-seeding");
//                     var existingUsers = await _context.Users.ToListAsync();
//                     _context.Users.RemoveRange(existingUsers);
//                     await _context.SaveChangesAsync();
//                 }
//
//                 _logger.LogInformation("Adding {count} new users to database", users.Count);
//                 await _context.Users.AddRangeAsync(users);
//                 await _context.SaveChangesAsync();
//                 _logger.LogInformation("Successfully seeded {count} users", users.Count);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error while seeding users");
//                 throw;
//             }
//         }
//
//         private async Task SeedHotelsAsync()
//         {
//             if (await _context.Hotels.AnyAsync())
//             {
//                 _logger.LogInformation("Hotels table already has data - skipping seeding");
//                 return;
//             }
//
//             var hotels = new List<Hotel>();
//             
//             // Create 8 hotels with variety
//             for (int i = 0; i < 8; i++)
//             {
//                 var name = _hotelNames[i % _hotelNames.Length];
//                 var location = _locations[i % _locations.Length];
//                 
//                 hotels.Add(new Hotel
//                 {
//                     Name = name,
//                     Description = $"{name} is a beautiful hotel located in {location} with excellent amenities and friendly staff.",
//                     Location = location,
//                     Rating = Math.Round((decimal)(_random.NextDouble() * 2 + 3), 1), // Rating between 3.0 and 5.0
//                     CreatedAt = _currentDate,
//                     IsDeleted = false
//                 });
//             }
//
//             await _context.Hotels.AddRangeAsync(hotels);
//             await _context.SaveChangesAsync();
//             _logger.LogInformation("Seeded {count} hotels", hotels.Count);
//         }
//         
//         private async Task SeedHotelPhotosAsync()
//         {
//             if (await _context.HotelPhotos.AnyAsync())
//             {
//                 _logger.LogInformation("Hotel photos table already has data - skipping seeding");
//                 return;
//             }
//
//             var hotels = await _context.Hotels.ToListAsync();
//             var photos = new List<HotelPhoto>();
//             
//             foreach (var hotel in hotels)
//             {
//                 // Add 3-5 photos per hotel
//                 int photosToAdd = _random.Next(3, 6);
//                 
//                 for (int i = 0; i < photosToAdd; i++)
//                 {
//                     var isMain = (i == 0); // First photo is main
//                     var photoUrl = _photoUrls[_random.Next(_photoUrls.Length)];
//                     var description = _photoDescriptions[_random.Next(_photoDescriptions.Length)];
//                     var publicId = $"booking-system/hotels/hotel_{hotel.Id}_photo_{i}";
//                     
//                     photos.Add(new HotelPhoto
//                     {
//                         HotelId = hotel.Id,
//                         Url = photoUrl,
//                         PublicId = publicId,
//                         Description = $"{hotel.Name} - {description}",
//                         IsMain = isMain,
//                         CreatedAt = _currentDate,
//                         IsDeleted = false
//                     });
//                 }
//             }
//             
//             await _context.HotelPhotos.AddRangeAsync(photos);
//             await _context.SaveChangesAsync();
//             _logger.LogInformation("Seeded {count} hotel photos", photos.Count);
//         }
//
//         private async Task SeedRoomTypesAsync()
//         {
//             if (await _context.RoomTypes.AnyAsync())
//             {
//                 _logger.LogInformation("Room types table already has data - skipping seeding");
//                 return;
//             }
//
//             var hotels = await _context.Hotels.ToListAsync();
//             var roomTypes = new List<RoomType>();
//             
//             foreach (var hotel in hotels)
//             {
//                 // Add 3-5 room types per hotel
//                 int typesToAdd = _random.Next(3, 6);
//                 
//                 for (int i = 0; i < typesToAdd; i++)
//                 {
//                     var typeName = _roomTypeNames[i % _roomTypeNames.Length];
//                     var bedCount = typeName switch
//                     {
//                         "Standard" => 1,
//                         "Deluxe" => 1,
//                         "Suite" => 2,
//                         "Family Room" => 2,
//                         "Penthouse" => 2,
//                         _ => 1
//                     };
//                     
//                     var area = typeName switch
//                     {
//                         "Standard" => 25m,
//                         "Deluxe" => 35m,
//                         "Suite" => 50m,
//                         "Family Room" => 60m,
//                         "Penthouse" => 80m,
//                         _ => 30m
//                     };
//
//                     roomTypes.Add(new RoomType
//                     {
//                         Name = typeName,
//                         Description = $"{typeName} room with {bedCount} bed(s) and {area}m² area",
//                         Capacity = bedCount,
//                         Area = area,
//                         Floor = _random.Next(1, 10),
//                         HotelId = hotel.Id,
//                         CreatedAt = _currentDate,
//                         IsDeleted = false
//                     });
//                 }
//             }
//             
//             await _context.RoomTypes.AddRangeAsync(roomTypes);
//             await _context.SaveChangesAsync();
//             _logger.LogInformation("Seeded {count} room types", roomTypes.Count);
//         }
//
//         private async Task SeedRoomPricingsAsync()
// {
//     if (await _context.RoomPricings.AnyAsync())
//     {
//         _logger.LogInformation("Room pricings table already has data - skipping seeding");
//         return;
//     }
//
//     var roomTypes = await _context.RoomTypes.ToListAsync();
//     var pricings = new List<RoomPricing>();
//     
//     foreach (var roomType in roomTypes)
//     {
//         // Use the BasePrice property from RoomType
//         var basePrice = roomType.BasePrice;
//         if (basePrice <= 0)
//         {
//             // Fallback if BasePrice is not set
//             basePrice = roomType.Name switch
//             {
//                 "Standard" => 100m,
//                 "Deluxe" => 150m,
//                 "Suite" => 200m,
//                 "Family Room" => 250m,
//                 "Penthouse" => 400m,
//                 _ => 120m
//             };
//         }
//         
//         // Add slight variation to price
//         basePrice += _random.Next(-20, 21);
//         
//         for (int i = 0; i < 30; i++)
//         {
//             var date = _currentDate.Date.AddDays(i);
//             var isWeekend = date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;
//             var price = basePrice;
//             
//             // Increase price on weekends
//             if (isWeekend)
//             {
//                 price += basePrice * 0.2m;
//             }
//             
//             pricings.Add(new RoomPricing
//             {
//                 RoomTypeId = roomType.Id, // Changed from RoomId to RoomTypeId
//                 Date = date,
//                 Price = Math.Round(price, 2),
//                 CreatedAt = _currentDate,
//                 IsDeleted = false
//             });
//         }
//     }
//     
//     await _context.RoomPricings.AddRangeAsync(pricings);
//     await _context.SaveChangesAsync();
//     _logger.LogInformation("Seeded {count} room pricings", pricings.Count);
// }
//
//     private async Task SeedBookingsAsync()
// {
//     bool isExisting = await _context.Bookings.AnyAsync();
//     if (isExisting)
//     {
//         _logger.LogInformation("Bookings table already has data - skipping seeding");
//         return;
//     }
//
//     var users = await _context.Users.Where(u => u.Role == (int)UserRole.Guest).ToListAsync();
//     var roomTypes = await _context.RoomTypes.Include(rt => rt.Hotel).ToListAsync();
//     var bookings = new List<Booking>();
//     
//     // Create 20 bookings
//     for (int i = 0; i < 20; i++)
//     {
//         var user = users[_random.Next(users.Count)];
//         var roomType = roomTypes[_random.Next(roomTypes.Count)];
//         
//         // Random duration between 1 and 7 days
//         var duration = _random.Next(1, 8);
//         
//         // Random check-in date between -15 days and +30 days from current date
//         var daysOffset = _random.Next(-15, 31);
//         var checkInDate = _currentDate.Date.AddDays(daysOffset);
//         var checkOutDate = checkInDate.AddDays(duration);
//         
//         // Determine booking status based on dates
//         BookingStatus status;
//         if (checkOutDate < _currentDate)
//         {
//             status = BookingStatus.Completed;
//         }
//         else if (checkInDate <= _currentDate && checkOutDate >= _currentDate)
//         {
//             status = BookingStatus.Active;
//         }
//         else if (checkInDate > _currentDate)
//         {
//             status = BookingStatus.Confirmed;
//         }
//         else
//         {
//             status = BookingStatus.Cancelled;
//         }
//         
//         // Calculate price based on room type's base price and duration
//         var totalPrice = duration * (roomType.BasePrice > 0 ? roomType.BasePrice : 100m);
//         
//         // Add random guests (1 to max room capacity)
//         var maxGuests = roomType.Capacity > 0 ? roomType.Capacity : 2;
//         var guestCount = _random.Next(1, maxGuests + 1);
//         
//         bookings.Add(new Booking
//         {
//             UserId = user.Id,
//             RoomTypeId = roomType.Id, // Changed from RoomId to RoomTypeId
//             HotelId = roomType.HotelId, // Added HotelId from the room type's hotel
//             CheckInDate = checkInDate,
//             CheckOutDate = checkOutDate,
//             GuestCount = guestCount,
//             TotalPrice = totalPrice,
//             Status = (int)status,
//             CreatedAt = _currentDate.AddDays(-(_random.Next(1, 30))), // Created 1-30 days ago
//             IsDeleted = false
//         });
//     }
//     
//     await _context.Bookings.AddRangeAsync(bookings);
//     await _context.SaveChangesAsync();
//     _logger.LogInformation("Seeded {count} bookings", bookings.Count);
// }
//     }
// }
