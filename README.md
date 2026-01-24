# 🏨 Hotel Booking System - REST API

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-512BD4?logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-9.0.0-512BD4?logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?logo=JSON-web-tokens&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache_Kafka-231F20?logo=apachekafka&logoColor=white)
![Cloudinary](https://img.shields.io/badge/Cloudinary-3448C5?logo=cloudinary&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=white)
![Postman](https://img.shields.io/badge/Postman-FF6C37?logo=postman&logoColor=white)

Welcome to the Hotel Booking System - a comprehensive REST API solution for managing hotel reservations, room type management, and guest services! 🌍

This robust system is built with ASP.NET Core 9, following Clean Architecture principles and implementing CQRS patterns with Redis caching and Kafka messaging for optimal performance and scalability.

## ✨ Features

### 🔐 Authentication & Authorization
- JWT-based authentication system
- Role-based access control (Admin, Manager, Guest)
- Secure user registration and login
- Password change functionality
- Cookie and Bearer token support

### 🏨 Hotel Management
- Complete hotel CRUD operations
- Advanced hotel search with multiple criteria
- Hotel photo management with Cloudinary integration
- Location-based services
- Rating and amenities management

### 🛏️ Room Type Management
- Room type categorization and management
- Dynamic room pricing system
- Room availability tracking
- Comprehensive room amenities
- Floor and capacity management

### 📅 Booking System
- Real-time room booking and availability
- Booking history and management
- Multiple booking status tracking (Pending, Active, Confirmed, Cancelled, Completed)
- Cancellation handling
- User-specific booking access control

### 🖼️ Media Management
- Hotel photo upload and management
- Cloudinary integration for image storage
- Image transformation and optimization
- Bulk photo upload capabilities
- Main photo designation

### 👥 User Management
- User profile management
- Advanced user search functionality
- Role-based permissions
- Account management for admins
- User search by email and username

### ⚡ Performance & Caching
- Redis distributed caching
- Cached service decorators for improved performance
- Cache invalidation strategies
- Configurable cache expiration

## 🏗️ Architecture

This system follows **Clean Architecture** principles with the following layers:

### Core Layers
- **Domain Layer**: Contains business entities, enums, and domain logic
- **Application Layer**: Implements business logic, DTOs, and service interfaces

### External Layers
- **API Layer**: Controllers handling HTTP requests and responses
- **Infrastructure Layer**: Database context, repositories, and external service integrations

### Design Patterns
- **CQRS (Command Query Responsibility Segregation)**: Separates read and write operations
- **Repository Pattern**: Abstracts data access logic
- **Decorator Pattern**: Implements caching layer without modifying core services
- **Dependency Injection**: Promotes loose coupling and testability

## 🛠️ Technology Stack

### Backend Framework
- **ASP.NET Core 9.0**: High-performance, cross-platform web API framework
- **Entity Framework Core 9.0.0**: Modern ORM for database operations
- **PostgreSQL**: Reliable and scalable database management

### Messaging & Events
- **Apache Kafka**: High-throughput distributed streaming platform
- **Confluent.Kafka**: .NET client for Apache Kafka

### Caching & Performance
- **Redis**: Distributed caching with StackExchange.Redis
- **IDistributedCache**: Abstraction layer for caching
- **Custom Cache Service**: Pattern-based cache invalidation

### Authentication & Security
- **JWT (JSON Web Tokens)**: Secure token-based authentication
- **ASP.NET Core Identity**: User management and role-based authorization

### External Services
- **Cloudinary**: Cloud-based image storage and transformation
- **Serilog**: Structured logging with multiple sinks

### Logging & Monitoring
- **Serilog.AspNetCore 9.0.0**: Structured logging framework
- **Serilog.Sinks.Console 6.0.0**: Console output for development
- **Serilog.Sinks.File 7.0.0**: File-based logging with daily rolling

### Documentation & Testing
- **Swashbuckle.AspNetCore 6.6.2**: Interactive API documentation
- **Postman**: API testing and collection management [link](https://www.postman.com/daniil-1257903/workspace/booking-api/collection/43932839-ad8c2ffd-604d-4799-a371-6644d3156661?action=share&creator=43932839)
- **xUnit 2.9.3**: Unit testing framework
- **Moq 4.20.72**: Mocking framework for testing

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/Hotel-Booking-Backend-API.git
cd BookingSystem.API
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=HotelBookingDB;Username=your_username;Password=your_password",
    "Redis": "localhost:6379"
  }
}
```

### 3. Configure External Services

Add your configuration to `appsettings.json`:

```json
{
  "JWT": {
    "Secret": "your-super-secret-key-here-with-at-least-32-characters",
    "DurationInDays": 7
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }  
}
```

### 4. Start Required Services

```bash
# Start Redis (using Docker)
docker run -d -p 6379:6379 redis:alpine

# Or install Redis locally and start the service
```

### 5. Run Database Migrations

```bash
dotnet ef database update --project BookingSystem.Infrastructure --startup-project BookingSystem.API
```

### 6. Start the Application

```bash
dotnet run --project BookingSystem.API
```

The API will be available at:
- **HTTP**: `http://localhost:5044`
- **Swagger UI**: `http://localhost:5044/swagger`

## 📚 API Documentation

### Authentication Endpoints

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| POST | `/api/auth/register` | Register a new user | Public |
| POST | `/api/auth/login` | Login user | Public |
| POST | `/api/auth/logout` | Logout user | Authenticated |
| GET | `/api/auth/current` | Get current user info | Authenticated |

### Hotel Management

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/hotels` | Get all hotels | Public |
| GET | `/api/hotels/{id}` | Get hotel by ID | Public |
| POST | `/api/hotels` | Create new hotel | Manager, Admin |
| PUT | `/api/hotels/{id}` | Update hotel | Manager, Admin |
| DELETE | `/api/hotels/{id}` | Delete hotel | Manager, Admin |
| GET | `/api/hotels/search` | Search hotels | Public |

### Room Management

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/rooms` | Get all rooms | Authenticated |
| GET | `/api/rooms/{id}` | Get room by ID | Authenticated |
| POST | `/api/rooms` | Create new room | Authenticated |
| PUT | `/api/rooms/{id}` | Update room | Authenticated |
| DELETE | `/api/rooms/{id}` | Delete room | Authenticated |

### Room Types

| Method | Endpoint                             | Description | Access |
|--------|--------------------------------------|-------------|--------|
| GET | `/api/room_types`                    | Get all room types | Public |
| GET | `/api/room_types/{id}`               | Get room type by ID | Public |
| GET | `/api/room_types/by_hotel/{hotelId}` | Get room types by hotel | Public |
| POST | `/api/room_types`                    | Create room type | Manager, Admin |
| PUT | `/api/room_types/{id}`               | Update room type | Manager, Admin |
| DELETE | `/api/room-types/{id}`               | Delete room type | Manager, Admin |

### Booking Management

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/bookings` | Get bookings (user's own or all for managers) | Authenticated |
| GET | `/api/bookings/{id}` | Get booking by ID | Authenticated |
| POST | `/api/bookings` | Create new booking | Authenticated |
| PUT | `/api/bookings/{id}` | Update booking | Authenticated |
| DELETE | `/api/bookings/{id}` | Cancel booking | Authenticated |
| GET | `/api/bookings/all` | Get all bookings | Manager, Admin |

### User Management

| Method | Endpoint                            | Description | Access |
|--------|-------------------------------------|-------------|--------|
| GET | `/api/users`                        | Get all users | Manager, Admin |
| GET | `/api/users/{id}`                   | Get user by ID | Authenticated (own) or Manager/Admin |
| POST | `/api/users`                        | Create new user | Admin |
| PUT | `/api/users/{id}`                   | Update user | Authenticated (own) or Admin |
| DELETE | `/api/users/{id}`                   | Delete user | Admin |
| GET | `/api/users/profile`                | Get current user profile | Authenticated |
| POST | `/api/users/change_password`        | Change password | Authenticated |
| GET | `/api/users/by_email/{email}`       | Get user by email | Manager, Admin |
| GET | `/api/users/by_username/{username}` | Get user by username | Manager, Admin |

### Hotel Photos

| Method | Endpoint                            | Description | Access |
|--------|-------------------------------------|-------------|--------|
| GET | `/api/hotel_photos`                 | Get all photos | Authenticated |
| GET | `/api/hotel_photos/{id}`            | Get photo by ID | Public |
| GET | `/api/hotel_photos/hotel/{hotelId}` | Get photos by hotel | Public |
| POST | `/api/hotel_photos/upload`          | Upload single photo | Public |
| POST | `/api/hotel_photos/upload/multiple` | Upload multiple photos | Public |
| PUT | `/api/hotel_photos/{id}`            | Update photo metadata | Public |
| DELETE | `/api/hotel_photos/{id}`            | Delete photo | Public |
| PUT | `/api/hotel_photos/{id}/set_main`   | Set main photo | Public |
| GET | `/api/hotel_photos/{id}/transform`  | Get transformed image URL | Public |
| POST | `/api/hotel_photos/sync/{hotelId}`  | Sync Cloudinary photos | Authenticated |

### Room Pricing

| Method | Endpoint                  | Description | Access |
|--------|---------------------------|-------------|--------|
| GET | `/api/room_pricings`      | Get all room pricing | Public |
| GET | `/api/room_pricings/{id}` | Get room pricing by ID | Public |
| POST | `/api/room_pricings`      | Create room pricing | Manager, Admin |
| PUT | `/api/room_pricings/{id}` | Update room pricing | Manager, Admin |
| DELETE | `/api/roomp_ricings/{id}` | Delete room pricing | Manager, Admin |


### Amenities

| Method | Endpoint                  | Description | Access |
|--------|---------------------------|---------|--------|
| GET | `/api/amenities`          | Get all amenities | Public |
| GET | `/api/amenities/{id}` | Get amenities by ID | Public |
| POST | `/api/amenities`      | Create amenities | Manager, Admin |
| PUT | `/api/amenities/{id}` | Update amenities | Manager, Admin |
| DELETE | `/api/amenities/{id}` | Delete amenities | Manager, Admin |

## 🧪 Testing

Run the test suite:

```bash
dotnet test
```

## 📁 Project Structure

```
BookingSystem/
├── BookingSystem.API/              # Web API layer
│   ├── Controllers/                # API controllers
│   ├── Program.cs                  # Application entry point
│   └── Properties/                 # Launch settings
├── BookingSystem.Application/      # Application layer
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Interfaces/                 # Service interfaces
│   ├── Services/                   # Business logic services
│   ├── Decorators/                 # Caching decorators
├── BookingSystem.Domain/           # Domain layer
│   ├── Entities/                   # Domain entities
│   ├── Enums/                      # Domain enumerations
│   ├── Interfaces/                 # Domain interfaces
│   └── Other/                      # Value objects and settings
├── BookingSystem.Infrastructure/   # Infrastructure layer
│   ├── Data/                       # Database context
│   ├── Repositories/               # Data access repositories
│   └── Services/                   # External service implementations
└── BookingSystem.Tests/            # Test projects
   
```

## 🔧 Configuration

### Environment Variables

Create an `appsettings.Development.json` file for local development:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=HotelBookingDB;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "JWT": {
    "Secret": "your-super-secret-key-here-with-at-least-32-characters-for-security",
    "DurationInDays": 7
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "booking-processor-group",
    "Topics": {
      "BookingRequests": "booking.requests",
      "BookingResults": "booking.results"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Docker Support

Build and run with Docker:

```bash
# Build the image
docker build -t hotel-booking-system .

# Run with Docker Compose (including Redis and PostgreSQL)
docker-compose up -d
```

## 📊 Caching Strategy

The system implements a comprehensive caching strategy using Redis:

### Cache Decorators
- **CachedBookingService**: Caches booking data with automatic invalidation
- **CachedHotelService**: Caches hotel information and search results
- **CachedUserService**: Caches user profiles and search results

### Cache Keys Pattern
```
bookings:all
bookings:user:{userId}
bookings:hotel:{hotelId}
hotels:all
hotels:search:{hash}
hotels:detail:{id}
users:profile:{id}
rooms:availability:{hotelId}:{date}
```

### Cache Invalidation
- Pattern-based invalidation (e.g., `bookings:*` when booking changes)
- Automatic TTL management
- Event-driven cache clearing

## 📨 Apache Kafka Integration

This project uses **Apache Kafka** for asynchronous booking processing. This ensures high availability and decoupling between the API receipt of a booking request and its actual processing in the database.

### 🏗 Workflow Architecture
1. **Producer**: When a booking request is made via the API, the system validates the request and publishes a message to a Kafka topic instead of writing directly to the database synchronously.
2. **Consumer**: A background `HostedService` (`BookingRequestConsumer`) listens to the topic.
3. **Processing**: The consumer picks up the message, performs necessary business logic, and persists the booking into PostgreSQL.


## 🔐 Security Features

- **JWT Authentication**: Stateless token-based authentication with configurable expiration
- **Role-based Authorization**: Granular permission control (Guest, Manager, Admin)
- **HTTPS Enforcement**: Secure data transmission
- **Password Hashing**: Secure password storage using built-in ASP.NET Core Identity
- **CORS Configuration**: Cross-origin request handling with specific origins
- **Input Validation**: Protection against malicious input with model validation
- **SQL Injection Protection**: Entity Framework Core parameterized queries

## 📈 Monitoring & Logging

The system includes comprehensive logging using Serilog:

### Logging Configuration
- **Console Output**: Colored console logs for development
- **File Logging**: Daily rolling files in `/Logs` directory
- **Structured Logging**: JSON-formatted logs for easy parsing
- **Log Levels**: Configurable minimum levels with Microsoft framework filtering

## 🌟 Future Enhancements

- [ ] Email notifications for bookings using background services
- [ ] Payment gateway integration (Stripe/PayPal)
- [ ] Real-time notifications with SignalR
- [ ] Advanced reporting and analytics dashboard
- [ ] Multi-language support (i18n)
---

⭐ **Star this repository if you found it helpful!** ⭐
