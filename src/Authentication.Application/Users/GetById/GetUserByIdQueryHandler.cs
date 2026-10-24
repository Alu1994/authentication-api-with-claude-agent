using Authentication.Application.Abstractions.Messaging;
using Authentication.Application.Abstractions.Results;

namespace Authentication.Application.Users.GetById;

public sealed class GetUserByIdQueryHandler(IUserRepository repository)
    : IQueryHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(query.Id, cancellationToken);

        return user is null ? UserErrors.NotFound : UserResponse.FromUser(user);
    }
}
