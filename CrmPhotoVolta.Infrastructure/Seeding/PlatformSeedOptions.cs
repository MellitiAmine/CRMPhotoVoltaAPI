namespace CrmPhotoVolta.Infrastructure.Seeding;

public sealed class PlatformSeedOptions
{
    public const string SectionName = "PlatformSeed";

    public bool Enabled { get; set; } = true;

    /// <summary>Operateur plateforme: toutes les societes, forfaits, sans rattachement obligatoire a une societe demo.</summary>
    public string PlatformAdminEmail { get; set; } = "plateforme@crm.local";

    public string PlatformAdminPassword { get; set; } = "ChangeMe123!";

    public bool CreateDemoSocieties { get; set; } = true;

    public string DemoSocietyFreeName { get; set; } = "Société test — Essai gratuit (3 mois)";

    public string DemoSocietyPaidName { get; set; } = "Société test — Abonnement 100 TND/mois";

    /// <summary>Admin uniquement sur la societe demo essai gratuit.</summary>
    public string DemoFreeSocietyAdminEmail { get; set; } = "admin.essai@crm.local";

    public string DemoFreeSocietyAdminPassword { get; set; } = "Demo123!";

    public string DemoFreeSocietyAdminFullName { get; set; } = "Admin société (essai gratuit)";

    /// <summary>Admin uniquement sur la societe demo payante.</summary>
    public string DemoPaidSocietyAdminEmail { get; set; } = "admin.payant@crm.local";

    public string DemoPaidSocietyAdminPassword { get; set; } = "Demo123!";

    public string DemoPaidSocietyAdminFullName { get; set; } = "Admin société (abonnement)";
}
