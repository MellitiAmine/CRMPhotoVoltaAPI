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

/// <summary>Overall project lifecycle.</summary>
public enum ProjectStatus
{
    Planned,
    InProgress,
    Done,
    Cancelled
}
