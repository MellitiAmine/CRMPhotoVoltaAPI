namespace CrmPhotoVolta.Domain.App;

/// <summary>Common lead status strings (pipeline may still use custom labels).</summary>
public static class LeadStatuses
{
    public const string Nouveau = "Nouveau";
    public const string Contacte = "Contacté";
    public const string Qualifie = "Qualifié";
    public const string Proposition = "Proposition";
    public const string Negociation = "Négociation";
    public const string Gagne = "Gagné";
    public const string Perdu = "Perdu";
    public const string Archive = "Archivé";
    public const string Installation = "Installation";
    public const string Converted = "Converti"; // Kept for conversion logic if relied upon
}

/// <summary>Typical deal pipeline stage names (deals may still use custom stages).</summary>
public static class DealStages
{
    public const string New = "New";
    public const string Won = "Won";
    public const string Lost = "Lost";
}
