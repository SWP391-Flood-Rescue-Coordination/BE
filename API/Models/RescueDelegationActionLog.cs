namespace Flood_Rescue_Coordination.API.Models;

public class RescueDelegationActionLog
{
    public long DelegationActionLogId { get; set; }
    public Guid ActionBatchId { get; set; }
    public int? RequestId { get; set; }
    public int? OperationId { get; set; }
    public int ActorUserId { get; set; }
    public int? MemberUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActionReason { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public string? OperationStatus { get; set; }
    public DateTime ActionAt { get; set; }
}
