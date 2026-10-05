using System.ComponentModel.DataAnnotations.Schema;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

[Table("user_action_audit")]
public class UserActionAudit : BaseEntity
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("user_action_type")]
    public UserActionType UserActionType { get; set; } = UserActionType.Unknown;

    [Column("is_success")]
    public bool IsSuccess { get; set; }

    public User User { get; set; } = null!;
}
