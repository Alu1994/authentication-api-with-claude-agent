using Authentication.Application.Abstractions.Validation;
using Authentication.Application.Roles;
using Authentication.Application.Roles.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(RoleRules.NameMaxLength).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(RoleRules.NameMaxLength).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(DescriptionRules.MaxLength);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(r => r.DeletedAt);

        // Nome unico (sem diferenciar maiusculas/minusculas) apenas entre roles ativas.
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ix_roles_normalized_name");

        // Soft delete: roles excluidas ficam invisiveis para as consultas padrao.
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
