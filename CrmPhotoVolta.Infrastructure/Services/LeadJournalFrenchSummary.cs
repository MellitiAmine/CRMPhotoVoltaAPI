using System.Text.Json;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>Builds a short French sentence for admins from journal action + metadata JSON.</summary>
internal static class LeadJournalFrenchSummary
{
    /// <summary>Short French text when metadata is missing or unparsable.</summary>
    public static string FallbackSummary(string action) => ActionFallback(action);

    public static string Build(
        string action,
        string? metadataJson,
        IReadOnlyDictionary<Guid, (string FullName, string RoleLabel)> users)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return ActionFallback(action);

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            return action switch
            {
                LeadJournalActions.CommercialAssigned => FormatCommercialAssigned(root, users),
                LeadJournalActions.LeadStatusChanged => FormatStatusChanged(root),
                LeadJournalActions.LeadTemperatureChanged => FormatTemperatureChanged(root),
                LeadJournalActions.LeadConverted => "Lead converti en client (détails dans le CRM).",
                LeadJournalActions.LeadMarkedWon => "Lead marqué gagné.",
                LeadJournalActions.LeadMarkedLost => "Lead marqué perdu.",
                LeadJournalActions.CalendarEventCreated => FormatCalendarCreated(root),
                LeadJournalActions.CalendarEventUpdated => FormatCalendarUpdated(root),
                LeadJournalActions.CalendarEventDeleted => FormatCalendarDeleted(root),
                LeadJournalActions.ActivityUpdated => FormatActivityUpdated(root),
                LeadJournalActions.ActivityDeleted => FormatActivityDeleted(root),
                LeadJournalActions.ActivityCreated => string.Empty, // legacy rows; UI hides or shows fallback
                _ => ActionFallback(action)
            };
        }
        catch
        {
            return ActionFallback(action);
        }
    }

    private static string ActionFallback(string action) =>
        action switch
        {
            LeadJournalActions.CommercialAssigned => "Changement d’assignation commercial.",
            LeadJournalActions.LeadStatusChanged => "Changement de statut du lead.",
            LeadJournalActions.LeadTemperatureChanged => "Changement de température du lead.",
            LeadJournalActions.LeadConverted => "Lead converti en client.",
            LeadJournalActions.LeadMarkedWon => "Lead marqué gagné.",
            LeadJournalActions.LeadMarkedLost => "Lead marqué perdu.",
            LeadJournalActions.CalendarEventCreated => "Événement calendrier créé.",
            LeadJournalActions.CalendarEventUpdated => "Événement calendrier modifié.",
            LeadJournalActions.CalendarEventDeleted => "Événement calendrier supprimé.",
            LeadJournalActions.ActivityCreated => "Activité enregistrée (historique).",
            LeadJournalActions.ActivityUpdated => "Activité modifiée.",
            LeadJournalActions.ActivityDeleted => "Activité supprimée.",
            _ => $"Événement enregistré ({action})."
        };

    private static string FormatCommercialAssigned(
        JsonElement root,
        IReadOnlyDictionary<Guid, (string FullName, string RoleLabel)> users)
    {
        static bool Empty(Guid? g) => g is null || g == Guid.Empty;
        var before = TryGuid(root, "beforeUserId");
        var after = TryGuid(root, "afterUserId");

        if (Empty(before) && Empty(after))
            return "L’assignation commercial a été mise à jour.";

        if (Empty(before) && !Empty(after))
            return $"Le lead est désormais suivi par {PersonPhrase(after, users)}.";

        if (!Empty(before) && Empty(after))
            return $"{CapitalizeFrench(PersonPhrase(before, users))} ne suit plus ce lead ; aucun commercial n’est assigné.";

        var a = PersonPhrase(before, users);
        var b = PersonPhrase(after, users);
        return $"{CapitalizeFrench(a)} a été remplacé par {b}.";
    }

    private static string PersonPhrase(Guid? userId, IReadOnlyDictionary<Guid, (string FullName, string RoleLabel)> users)
    {
        if (userId is null || userId == Guid.Empty)
            return string.Empty;
        if (!users.TryGetValue(userId.Value, out var u))
            return "un utilisateur non répertorié dans cette société";
        return PersonFr(u.FullName, u.RoleLabel);
    }

    private static string PersonFr(string? fullName, string? roleLabel)
    {
        var n = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        var r = (roleLabel ?? "").Trim().ToLowerInvariant();
        if (r.Contains("admin"))
            return n is null ? "l’administrateur" : $"l’administrateur {n}";
        if (r.Contains("manager"))
            return n is null ? "un manager" : $"le manager {n}";
        if (r.Contains("commercial"))
            return n is null ? "un commercial" : $"le commercial {n}";
        return n is null ? "un collaborateur" : n;
    }

    private static string CapitalizeFrench(string phrase)
    {
        if (string.IsNullOrEmpty(phrase))
            return phrase;
        if (phrase.Length >= 2 && (phrase.StartsWith("l’", StringComparison.Ordinal) || phrase.StartsWith("L’", StringComparison.Ordinal)))
            return "L’" + phrase[2..];
        return char.ToUpperInvariant(phrase[0]) + phrase[1..];
    }

    private static string CalendarTypeFr(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return string.Empty;
        return type.Trim().ToLowerInvariant() switch
        {
            "meeting" => "réunion",
            "reminder" => "rappel",
            "activity" => "activité",
            _ => type.Trim()
        };
    }

    private static string FormatStatusChanged(JsonElement root)
    {
        var before = GetString(root, "before") ?? "—";
        var after = GetString(root, "after") ?? "—";
        return $"Statut du lead : « {before} » → « {after} ».";
    }

    private static string FormatTemperatureChanged(JsonElement root)
    {
        var temp = GetString(root, "temperature") ?? "—";
        return $"Température : {temp}.";
    }

    private static string FormatCalendarCreated(JsonElement root)
    {
        var title = GetString(root, "title") ?? "Événement";
        var typeFr = CalendarTypeFr(GetString(root, "type"));
        return string.IsNullOrEmpty(typeFr)
            ? $"Création au calendrier : « {title} »."
            : $"Création au calendrier ({typeFr}) : « {title} ».";
    }

    private static string FormatCalendarUpdated(JsonElement root)
    {
        var title = GetString(root, "title") ?? "Événement";
        var typeFr = CalendarTypeFr(GetString(root, "type"));
        return string.IsNullOrEmpty(typeFr)
            ? $"Modification au calendrier : « {title} »."
            : $"Modification au calendrier ({typeFr}) : « {title} ».";
    }

    private static string FormatCalendarDeleted(JsonElement root)
    {
        var title = GetString(root, "title") ?? "Événement";
        var typeFr = CalendarTypeFr(GetString(root, "type"));
        return string.IsNullOrEmpty(typeFr)
            ? $"Suppression au calendrier : « {title} »."
            : $"Suppression au calendrier ({typeFr}) : « {title} ».";
    }

    private static string FormatActivityUpdated(JsonElement root)
    {
        if (!TryGetPropertyInsensitive(root, "before", out var before) || !TryGetPropertyInsensitive(root, "after", out var after))
            return "Activité modifiée (détails conservés dans l’historique).";

        var bt = ActivityTypeFr(GetString(before, "type"));
        var at = ActivityTypeFr(GetString(after, "type"));
        var notesChanged = !string.Equals(GetString(before, "notes"), GetString(after, "notes"), StringComparison.Ordinal);
        var ratingChanged = GetInt(before, "rating") != GetInt(after, "rating");
        var parts = new List<string> { $"type : {bt} → {at}" };
        if (notesChanged) parts.Add("notes mises à jour");
        if (ratingChanged) parts.Add("évaluation mise à jour");
        return "Activité modifiée — " + string.Join(", ", parts) + ".";
    }

    private static string FormatActivityDeleted(JsonElement root)
    {
        var t = ActivityTypeFr(GetString(root, "type"));
        var notes = GetString(root, "notes");
        var rating = GetInt(root, "rating");
        var tail = string.IsNullOrWhiteSpace(notes) ? "" : $" Dernières notes : « {Truncate(notes!, 120)} ».";
        var r = rating is > 0 ? $" Évaluation : {rating}/5." : "";
        return $"Activité supprimée : {t}.{r}{tail}";
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s[..max] + "…";
    }

    private static string ActivityTypeFr(string? typeStr)
    {
        if (string.IsNullOrWhiteSpace(typeStr))
            return "activité";
        if (Enum.TryParse<LeadActivityType>(typeStr, ignoreCase: true, out var t))
            return t switch
            {
                LeadActivityType.Call => "appel",
                LeadActivityType.WhatsApp => "WhatsApp",
                LeadActivityType.Sms => "SMS",
                LeadActivityType.MeetingScheduled => "réunion planifiée",
                LeadActivityType.TechnicalVisit => "visite technique",
                LeadActivityType.InfoRequest => "demande d’info",
                LeadActivityType.QuoteRequest => "demande de devis",
                LeadActivityType.Negotiation => "négociation",
                LeadActivityType.StrongBuyingSignal => "signal d’achat",
                LeadActivityType.Note => "note interne",
                _ => typeStr!
            };
        return typeStr!;
    }

    private static Guid? TryGuid(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var p) || p.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (p.ValueKind == JsonValueKind.String && Guid.TryParse(p.GetString(), out var g))
            return g;
        return null;
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var p) || p.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number => p.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => p.ToString()
        };
    }

    private static bool TryGetPropertyInsensitive(JsonElement root, string name, out JsonElement value)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static int? GetInt(JsonElement root, string name)
    {
        if (!TryGetPropertyInsensitive(root, name, out var p) || p.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
            return i;
        return null;
    }

    /// <summary>Collects user ids embedded in known journal metadata shapes (for name resolution).</summary>
    public static void CollectUserIds(string action, string? metadataJson, ISet<Guid> sink)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return;
        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (action == LeadJournalActions.CommercialAssigned)
            {
                AddIfGuid(sink, TryGuid(root, "beforeUserId"));
                AddIfGuid(sink, TryGuid(root, "afterUserId"));
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void AddIfGuid(ISet<Guid> sink, Guid? g)
    {
        if (g is { } v && v != Guid.Empty) sink.Add(v);
    }
}
