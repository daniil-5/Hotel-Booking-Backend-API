namespace BookingSystem.Application.Exceptions;

/// <summary>
/// Регистрация пользователя с уже занятой почтой либо именем. Код 409.
/// </summary>
public class DuplicateUserException : Exception
{
    public DuplicateUserException(string message) : base(message) { }
}
