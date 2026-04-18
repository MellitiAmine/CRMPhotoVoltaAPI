namespace CrmPhotoVolta.Domain.App;

/// <summary>Persisted automation recommendation before any outbound WhatsApp/call action.</summary>
public class WhatsAppRecommendation : SocietyScopedEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public WhatsAppActionType ActionType { get; set; }

    public double Sd { get; set; }
    public LeadPriority Priority { get; set; }
    public LeadTemperature Temperature { get; set; }

    public bool IsSent { get; set; }
}
