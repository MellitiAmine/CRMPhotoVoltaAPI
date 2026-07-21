namespace CrmPhotoVolta.Domain.App;

/// <summary>
/// One clock punch (entrée or sortie) for a commercial on a given work day.
/// Multiple punches per day are allowed (e.g. 08:00 in, 12:00 out, 14:00 in, 17:00 out).
/// </summary>
public class CommercialTimeEntry : SocietyScopedEntity
{
    public Guid CommercialProfileId { get; set; }
    public CommercialProfile? CommercialProfile { get; set; }

    /// <summary>Calendar date of the punch (local society date).</summary>
    public DateOnly WorkDate { get; set; }

    public CommercialPunchType PunchType { get; set; }

    public TimeOnly Time { get; set; }

    public string? Notes { get; set; }
}
