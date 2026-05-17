namespace CrmPhotoVolta.Domain.App;

/// <summary>Quote lifecycle: Draft → Sent → Accepted | Rejected, or Converted to project.</summary>
public enum QuoteStatus
{
    Draft,
    Sent,
    Accepted,
    Rejected,
    Converted
}

/// <summary>Field installation workflow.</summary>
public enum InstallationStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>Tasks on a project.</summary>
public enum CrmTaskStatus
{
    Open,
    Done,
    Cancelled
}

/// <summary>Progress per configured project stage.</summary>
public enum ProjectStageTrackingStatus
{
    Pending,
    Done
}

/// <summary>Commercial contract types.</summary>
public enum ContractType
{
    Installation,
    Maintenance,
    Warranty,
    Financing,
    Other
}

/// <summary>Commercial contract lifecycle.</summary>
public enum ContractStatus
{
    Draft,
    SentToClient,
    Signed,
    Cancelled
}

/// <summary>Invoice lifecycle.</summary>
public enum InvoiceStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}

/// <summary>Payment methods.</summary>
public enum PaymentMethod
{
    BankTransfer,
    Cash,
    Cheque,
    CreditCard,
    Other
}

/// <summary>Project document categories.</summary>
public enum ProjectDocumentType
{
    Quote,
    Contract,
    Invoice,
    TechnicalStudy,
    STEG,
    InstallationPhoto,
    ClientDocument,
    SAV,
    Other
}

/// <summary>Photovoltaic project lifecycle (persisted as string in DB).</summary>
public enum ProjectStatus
{
    New,
    Study,
    TechnicalVisit,
    QuoteSent,
    Negotiation,
    Approved,
    Planning,
    Installation,
    WaitingSteg,
    Activated,
    Completed,
    Sav,
    Cancelled,

    /// <summary>Legacy value — migrate to <see cref="New"/>.</summary>
    Planned = New,

    /// <summary>Legacy value — migrate to <see cref="Installation"/>.</summary>
    InProgress = Installation,

    /// <summary>Legacy value — migrate to <see cref="Completed"/>.</summary>
    Done = Completed
}
