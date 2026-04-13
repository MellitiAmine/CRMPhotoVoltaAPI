namespace CrmPhotoVolta.Domain.App;

/// <summary>Common lead status strings (pipeline may still use custom labels).</summary>
public static class LeadStatuses
{
    public const string New = "New";
    public const string Converted = "Converted";
    public const string Won = "Won";
    public const string Lost = "Lost";
}

/// <summary>Typical deal pipeline stage names (deals may still use custom stages).</summary>
public static class DealStages
{
    public const string New = "New";
    public const string Won = "Won";
    public const string Lost = "Lost";
}
