using FMS.Communication.Common;
using FMS.Communication.Features.Auth.Dtos;
using FMS.Communication.Features.Auth.Queries;
using FMS.Web.Api.Bussiness.Rules.Abstractions;
using FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;
using FMS.Web.Api.Data.Repositories;

namespace FMS.Web.Api.Bussiness.Rules.Features.Auth.Queries;

public class GetLoggedUserQueryHandler(
    IUserRepository users,
    ILoggedUserBuilder userBuilder) : IQueryHandler<GetLoggedUserQuery, LoggedUserDto>
{
    public async Task<Result<LoggedUserDto>> HandleAsync(GetLoggedUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<LoggedUserDto>.Failure(ErrorType.NotFound, "Usuário não encontrado.");

        return Result<LoggedUserDto>.Success(await userBuilder.BuildAsync(user, cancellationToken));
    }
}
