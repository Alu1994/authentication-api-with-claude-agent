using Authentication.Application.Permissions;
using Authentication.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthenticationDbContext).Assembly);
}
