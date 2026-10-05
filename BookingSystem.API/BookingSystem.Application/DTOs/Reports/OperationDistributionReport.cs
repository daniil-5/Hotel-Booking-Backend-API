using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTOs.Reports;

public class OperationDistributionReport
{
    public UserActionType ActionType { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}
