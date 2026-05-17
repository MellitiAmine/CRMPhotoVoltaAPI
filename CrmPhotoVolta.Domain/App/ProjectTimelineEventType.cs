namespace CrmPhotoVolta.Domain.App;

public enum ProjectTimelineEventType
{
    ProjectCreated = 0,
    StatusChanged = 1,
    InstallationPlanned = 2,
    InstallationStarted = 3,
    InstallationCompleted = 4,
    ContractGenerated = 5,
    InvoiceGenerated = 6,
    PaymentReceived = 7,
    SAVCreated = 8,
    DocumentUploaded = 9,
    CommentAdded = 10,
    TaskCreated = 11,
    TaskCompleted = 12,
    ClientApproved = 13,
    Note = 14
}
