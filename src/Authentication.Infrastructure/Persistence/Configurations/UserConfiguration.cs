using Authentication.Application.Users;
using Authentication.Application.Users.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Name).HasMaxLength(UserRules.NameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(UserRules.EmailMaxLength).IsRequired();
        builder.Property(u => u.Cpf).HasMaxLength(Cpf.Length).IsFixedLength().IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(UserRules.PhoneMaxLength).IsRequired();
        builder.Property(u => u.BirthDate).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(u => u.DeletedAt);

        // Unicidade global (inclui excluidos): e-mail e CPF nao podem ser reaproveitados.
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Cpf).IsUnique();

        // Soft delete: usuarios excluidos ficam invisiveis para as consultas padrao.
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
