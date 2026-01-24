using System.Text.Json;
using BookingSystem.Application.DTOs.Commands;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Settings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BookingSystem.API.BackgroundServices
{
    public class BookingRequestConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaSettings _settings;
        private readonly ILogger<BookingRequestConsumer> _logger;
        private readonly string _requestTopic;
        private readonly string _resultTopic;

        public BookingRequestConsumer(
            IServiceProvider serviceProvider,
            IOptions<KafkaSettings> settings,
            ILogger<BookingRequestConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
            _requestTopic = _settings.Topics.BookingRequests; // "booking.requests"
            _resultTopic = _settings.Topics.BookingResults;   // "booking.results"
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task StartConsumerLoop(CancellationToken token)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId, 
                AutoOffsetReset = AutoOffsetReset.Earliest, 
                EnableAutoCommit = false 
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            
            try
            {
                consumer.Subscribe(_requestTopic);
                _logger.LogInformation("Kafka Consumer started listening on topic: {Topic}", _requestTopic);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(token);
                        
                        if (consumeResult?.Message == null) continue;

                        var messageValue = consumeResult.Message.Value;
                        _logger.LogInformation("Received booking request. Key: {Key}", consumeResult.Message.Key);
                        
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            await ProcessBookingRequest(messageValue, scope);
                        }
                        
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError(e, "Error occurred while consuming Kafka message");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "General error processing booking request");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka Consumer stopping...");
                consumer.Close();
            }
        }

        private async Task ProcessBookingRequest(string jsonMessage, IServiceScope scope)
        {
            CreateBookingCommand? command;
            try 
            {
                command = JsonSerializer.Deserialize<CreateBookingCommand>(jsonMessage);
                if (command == null) throw new JsonException("Deserialized command is null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize message: {Json}", jsonMessage);
                return;
            }
            
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
            var roomTypeRepo = scope.ServiceProvider.GetRequiredService<IRepository<RoomType>>();
            var pricingRepo = scope.ServiceProvider.GetRequiredService<IRepository<RoomPricing>>();
            var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

            _logger.LogInformation("Processing Booking Request {TrackingId} for RoomType {RoomTypeId}", command.TrackingId, command.RoomTypeId);
            
            var roomType = await roomTypeRepo.GetByIdAsync(command.RoomTypeId);
            if (roomType == null)
            {
                await SendResultAsync(producer, command.TrackingId, false, "Room type not found");
                return;
            }

            if (command.GuestCount > roomType.Capacity)
            {
                await SendResultAsync(producer, command.TrackingId, false, $"Max capacity is {roomType.Capacity}");
                return;
            }
            
            var overlappingBookings = await bookingRepo.CountAsync(b => 
                b.RoomTypeId == command.RoomTypeId &&
                b.Status != (int)BookingStatus.Cancelled &&
                b.CheckInDate < command.CheckOutDate &&
                b.CheckOutDate > command.CheckInDate
            );

            if (overlappingBookings >= roomType.Count)
            {
                _logger.LogWarning("Booking rejected: No rooms available. TrackingId: {TrackingId}", command.TrackingId);
                await SendResultAsync(producer, command.TrackingId, false, "No rooms available for selected dates");
                return;
            }
            
            decimal totalPrice = await CalculatePrice(pricingRepo, roomType, command.CheckInDate, command.CheckOutDate);
            
            var booking = new Booking
            {
                UserId = command.UserId,
                HotelId = command.HotelId,
                RoomTypeId = command.RoomTypeId,
                CheckInDate = command.CheckInDate,
                CheckOutDate = command.CheckOutDate,
                GuestCount = command.GuestCount,
                Status = (int)BookingStatus.Confirmed,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow,
                TrackingId = command.TrackingId
            };

            await bookingRepo.AddAsync(booking);
            
            _logger.LogInformation("Booking created successfully. ID: {BookingId}, TrackingId: {TrackingId}", booking.Id, command.TrackingId);
            
            await SendResultAsync(producer, command.TrackingId, true, "Booking Confirmed", booking.Id);
        }

        private async Task SendResultAsync(IKafkaProducer producer, Guid trackingId, bool success, string message, int? bookingId = null)
        {
            var resultEvent = new
            {
                TrackingId = trackingId,
                Success = success,
                Message = message,
                BookingId = bookingId,
                ProcessedAt = DateTime.UtcNow
            };
            
            await producer.SendMessageAsync(_resultTopic, trackingId.ToString(), resultEvent);
        }

        private async Task<decimal> CalculatePrice(IRepository<RoomPricing> pricingRepo, RoomType roomType, DateTime checkIn, DateTime checkOut)
        {
            decimal totalPrice = 0;
            int nightCount = (int)(checkOut.Date - checkIn.Date).TotalDays;
            
            var pricingRecords = await pricingRepo.GetAllAsync(rp => 
                rp.RoomTypeId == roomType.Id &&
                rp.Date >= checkIn.Date &&
                rp.Date < checkOut.Date);

            if (pricingRecords.Any())
            {
                var coveredDates = pricingRecords.Select(p => p.Date.Date).ToHashSet();
                totalPrice += pricingRecords.Sum(p => p.Price);
                
                for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
                {
                    if (!coveredDates.Contains(date))
                        totalPrice += roomType.BasePrice;
                }
            }
            else
            {
                totalPrice = roomType.BasePrice * nightCount;
            }
            
            return totalPrice;
        }
    }
}