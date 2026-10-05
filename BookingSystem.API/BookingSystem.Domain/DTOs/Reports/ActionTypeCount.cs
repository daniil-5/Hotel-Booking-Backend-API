using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.DTOs.Reports;

public class ActionTypeCount
{
    public UserActionType ActionType { get; set; }
    public int Count { get; set; }
}
