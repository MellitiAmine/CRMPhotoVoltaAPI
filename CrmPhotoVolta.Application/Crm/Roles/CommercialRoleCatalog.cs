namespace CrmPhotoVolta.Application.Crm.Roles;

/// <summary>Suggested CRM role names for seeding or documentation (society-scoped roles in core schema).</summary>
public static class CommercialRoleCatalog
{
    public static readonly IReadOnlyList<string> SuggestedNames = new[]
    {
        "super_admin", "ceo", "operations_manager",
        "sales_director", "sales_manager", "sales_executive", "account_manager",
        "technical_manager", "technician", "installer",
        "finance", "support_agent", "dispatcher",
        "viewer", "auditor"
    };
}
