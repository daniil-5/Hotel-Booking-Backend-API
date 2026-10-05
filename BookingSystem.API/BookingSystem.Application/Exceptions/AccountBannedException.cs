namespace BookingSystem.Application.Exceptions;

public class AccountBannedException : Exception
{
    public AccountBannedException(string message) : base(message) { }
}
