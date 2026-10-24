using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Paging;
using Authentication.Application.Abstractions.Results;
using Authentication.Application.Auth.Login;
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

        return services;
    }
}
