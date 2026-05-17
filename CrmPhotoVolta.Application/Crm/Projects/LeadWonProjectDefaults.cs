namespace CrmPhotoVolta.Application.Crm.Projects;

public static class LeadWonProjectDefaults
{
    public static readonly (string Title, string? Description, int DueDaysFromNow)[] DefaultTasks =
    {
        ("Visite technique", "Planifier et réaliser la visite technique sur site.", 7),
        ("Validation toiture", "Contrôle structure / orientation / ombrage.", 10),
        ("Dossier STEG", "Préparer le dossier raccordement STEG.", 14),
        ("Planification installation", "Bloquer équipe et matériel.", 21),
        ("Validation finale client", "PV ready / mise en service.", 30)
    };
}
