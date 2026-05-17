namespace CrmPhotoVolta.Application.Crm.Leads;

/// <summary>Stable action keys for <c>LeadJournalEntryDto.Action</c> (clients and reporting).</summary>
public static class LeadJournalActions
{
    public const string CommercialAssigned = "commercial.assigned";
    public const string LeadStatusChanged = "lead.status_changed";
    public const string LeadTemperatureChanged = "lead.temperature_changed";
    public const string LeadConverted = "lead.converted";
    public const string LeadMarkedWon = "lead.marked_won";
    public const string LeadMarkedLost = "lead.marked_lost";
    public const string ActivityCreated = "activity.created";
    public const string ActivityUpdated = "activity.updated";
    public const string ActivityDeleted = "activity.deleted";
    public const string CalendarEventCreated = "calendar.event_created";
    public const string CalendarEventUpdated = "calendar.event_updated";
    public const string CalendarEventDeleted = "calendar.event_deleted";
}
