namespace CrmPhotoVolta.Domain.App;

public enum LeadActivityType
{
    Call = 1,
    WhatsApp = 2,
    Sms = 3,
    MeetingScheduled = 4,
    TechnicalVisit = 5,

    InfoRequest = 10,
    QuoteRequest = 11,
    Negotiation = 12,
    StrongBuyingSignal = 13,

    Assignment = 100,
    StatusChange = 101,
    Note = 102,
    Converted = 103
}
