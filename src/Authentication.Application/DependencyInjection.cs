using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Auth.Login;
using Authentication.Application.Permissions;
using Authentication.Application.Permissions.Create;
using Authentication.Application.Permissions.Delete;
using Authentication.Application.Permissions.GetById;
using Authentication.Application.Permissions.List;
using Authentication.Application.Roles;
using Authentication.Application.Roles.Create;
using Authentication.Application.Roles.Delete;
using Authentication.Application.Roles.GetById;
using Authentication.Application.Roles.List;
using Authentication.Application.Roles.RolePermissions.Add;
using Authentication.Application.Roles.RolePermissions.List;
using Authentication.Application.Roles.RolePermissions.Remove;
using Authentication.Application.Roles.RolePermissions.Replace;
using Authentication.Application.Roles.Update;
using Authentication.Application.Users;
using Authentication.Application.Users.ChangePassword;
using Authentication.Application.Users.Create;
using Authentication.Application.Users.Delete;
using Authentication.Application.Users.GetById;
using Authentication.Application.Users.List;
using Authentication.Application.Users.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Authentication.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICommandHandler<CreateUserCommand, Result<UserResponse>>, CreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCommand, Result>, UpdateUserCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, Result>, ChangePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCommand, Result>, DeleteUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, Result<LoginResponse>>, LoginCommandHandler>();

        services.AddScoped<IQueryHandler<GetUserByIdQuery, Result<UserResponse>>, GetUserByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListUsersQuery, Result<PagedResponse<UserResponse>>>, ListUsersQueryHandler>();

        services.AddScoped<ICommandHandler<CreatePermissionCommand, Result<PermissionResponse>>,
            CreatePermissionCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePermissionCommand, Result>, DeletePermissionCommandHandler>();
        services.AddScoped<IQueryHandler<GetPermissionByIdQuery, Result<PermissionResponse>>,
            GetPermissionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListPermissionsQuery, Result<PagedResponse<PermissionResponse>>>,
            ListPermissionsQueryHandler>();

        services.AddScoped<ICommandHandler<CreateRoleCommand, Result<RoleResponse>>, CreateRoleCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateRoleCommand, Result>, UpdateRoleCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteRoleCommand, Result>, DeleteRoleCommandHandler>();
        services.AddScoped<IQueryHandler<GetRoleByIdQuery, Result<RoleResponse>>, GetRoleByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListRolesQuery, Result<PagedResponse<RoleResponse>>>, ListRolesQueryHandler>();

        services.AddScoped<ICommandHandler<AddRolePermissionCommand, Result>, AddRolePermissionCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveRolePermissionCommand, Result>, RemoveRolePermissionCommandHandler>();
        services.AddScoped<ICommandHandler<ReplaceRolePermissionsCommand, Result>,
            ReplaceRolePermissionsCommandHandler>();
        services.AddScoped<IQueryHandler<ListRolePermissionsQuery, Result<IReadOnlyList<PermissionResponse>>>,
            ListRolePermissionsQueryHandler>();

        return services;
    }
}
