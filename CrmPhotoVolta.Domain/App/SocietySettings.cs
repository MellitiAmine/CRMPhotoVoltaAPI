namespace CrmPhotoVolta.Domain.App;

/// <summary>One logical row per society (CRM preferences, feature flags JSON).</summary>
public class SocietySettings : SocietyScopedEntity
{
    public string DataJson { get; set; } = "{}";
}
