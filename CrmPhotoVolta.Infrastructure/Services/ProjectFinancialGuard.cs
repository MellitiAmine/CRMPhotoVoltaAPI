using CrmPhotoVolta.Application.Exceptions;

namespace CrmPhotoVolta.Infrastructure.Services;

internal static class ProjectFinancialGuard
{
    public static Guid ResolveClientId(Guid? requestClientId, Guid projectClientId)
    {
        if (requestClientId is { } cid && cid != Guid.Empty)
        {
            if (cid != projectClientId)
                throw new AppException(
                    "CLIENT_PROJECT_MISMATCH",
                    "Client does not match the project's client.",
                    400);
            return cid;
        }

        if (projectClientId == Guid.Empty)
            throw new AppException("CLIENT_REQUIRED", "Project has no client.", 400);

        return projectClientId;
    }
}
