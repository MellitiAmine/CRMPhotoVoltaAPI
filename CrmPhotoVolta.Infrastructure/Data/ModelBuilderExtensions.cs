using System.Reflection;
using CrmPhotoVolta.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Data;

internal static class ModelBuilderExtensions
{
    public static ModelBuilder ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(EntityBase).IsAssignableFrom(clrType) || clrType.IsAbstract)
                continue;

            typeof(ModelBuilderExtensions)
                .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(clrType)
                .Invoke(null, new object[] { modelBuilder });
        }

        return modelBuilder;
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : EntityBase
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}
