namespace BookingSystem.Application.Exceptions;

/// <summary>
/// Конфликт бронирования: пересечение дат с активной бронью либо нарушение
/// ограничений целостности на стороне базы данных. Отображается кодом 409.
/// </summary>
public class BookingConflictException : Exception
{
    public BookingConflictException(string message) : base(message) { }
}
