using Authentication.Application.Permissions;
using Authentication.Application.Permissions.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Resource).HasMaxLength(PermissionRules.SegmentMaxLength).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(PermissionRules.SegmentMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(PermissionRules.DescriptionMaxLength);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(p => p.DeletedAt);

        // "resource:action" e derivado; nao e persistido.
        builder.Ignore(p => p.Name);

        // Unicidade apenas entre permissoes ativas: o mesmo par pode ser recriado apos a exclusao.
        builder.HasIndex(p => new { p.Resource, p.Action })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ix_permissions_resource_action");

        // Soft delete: permissoes excluidas ficam invisiveis para as consultas padrao.
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
