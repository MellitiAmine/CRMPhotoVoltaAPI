namespace CrmPhotoVolta.Infrastructure.Services;

internal static class QuoteLinkResolver
{
    public static Guid? Normalize(Guid? id) =>
        id is { } g && g != Guid.Empty ? g : null;
}
