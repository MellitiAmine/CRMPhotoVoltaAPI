using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Automation;

/// <summary>Pure SD → next action (testable, no I/O).</summary>
public static class LeadSdDecisionEngine
{
    public static WhatsAppActionType ResolveAction(double sd)
    {
        if (sd >= LeadSdDecisionThresholds.CallSuggestedMinSd)
            return WhatsAppActionType.CallSuggested;
        if (sd >= LeadSdDecisionThresholds.WhatsAppMessageMinSd)
            return WhatsAppActionType.WhatsAppMessage;
        return WhatsAppActionType.Nurture;
    }
}
