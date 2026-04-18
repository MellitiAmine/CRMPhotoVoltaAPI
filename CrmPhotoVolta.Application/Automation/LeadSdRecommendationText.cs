using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Automation;

public static class LeadSdRecommendationText
{
    public static string BuildTitle(WhatsAppActionType action) =>
        action switch
        {
            WhatsAppActionType.CallSuggested => "Action: appel prioritaire",
            WhatsAppActionType.WhatsAppMessage => "Action: message WhatsApp",
            _ => "Action: nurturing"
        };

    public static string BuildBody(Lead lead, double sd, WhatsAppActionType action)
    {
        var line1 = $"Lead «{lead.Name}» — SD={sd:F1}.";
        var line2 = action switch
        {
            WhatsAppActionType.CallSuggested => "Recommandation: appeler le prospect en priorité.",
            WhatsAppActionType.WhatsAppMessage => "Recommandation: relance WhatsApp.",
            _ => "Recommandation: continuer le nurturing (contenu, séquences)."
        };
        return $"{line1} {line2}";
    }
}
