using FMS.Communication.Common;
using FMS.Communication.Features.Auth.Commands;
using FMS.Communication.Features.Auth.Dtos;
using FMS.Domain.entities;
using FMS.Web.Api.Bussiness.Rules.Abstractions;
using FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;
using FMS.Web.Api.Data.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FMS.Web.Api.Bussiness.Rules.Features.Auth.Commands;

public class LoginCommandHandler(
    IUserRepository users,
    IUserSessionRepository sessions,
    IUserLoginAttemptRepository attempts,
    IJwtTokenService tokens,
    ILoggedUserBuilder userBuilder,
    JwtOptions jwtOptions) : ICommandHandler<LoginCommand, LoginResultDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private const string InvalidCredentials = "Login ou senha inválidos.";

    private static readonly PasswordHasher<User> Hasher = new();

    public async Task<Result<LoginResultDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Login) || string.IsNullOrWhiteSpace(command.Password))
            return Result<LoginResultDto>.Failure(ErrorType.Validation, "Informe login e senha.");

        var user = await users.GetByLoginOrEmailAsync(command.Login, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await RegisterAttemptAsync(command, user?.Id, success: false, cancellationToken);
            return Result<LoginResultDto>.Failure(ErrorType.Unauthorized, InvalidCredentials);
        }

        if (user.LockedUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            await RegisterAttemptAsync(command, user.Id, success: false, cancellationToken);
            return Result<LoginResultDto>.Failure(ErrorType.Locked, "Usuário bloqueado temporariamente. Tente novamente mais tarde.");
        }

        var verification = Hasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockDuration);
                user.FailedAttempts = 0;
            }

            await users.UpdateAsync(user, cancellationToken);
            await RegisterAttemptAsync(command, user.Id, success: false, cancellationToken);
            return Result<LoginResultDto>.Failure(ErrorType.Unauthorized, InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = Hasher.HashPassword(user, command.Password);

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        var (refreshToken, refreshHash) = tokens.CreateRefreshToken();
        var session = await sessions.AddAsync(new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = refreshHash,
            RefreshExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent
        }, cancellationToken);

        await RegisterAttemptAsync(command, user.Id, success: true, cancellationToken);

        var loggedUser = await userBuilder.BuildAsync(user, cancellationToken);
        var (accessToken, accessExpiresAt) = tokens.CreateAccessToken(user, session.Id, loggedUser.Permissions);

        return Result<LoginResultDto>.Success(new LoginResultDto
        {
            AccessToken = accessToken,
            AccessExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshExpiresAt = session.RefreshExpiresAt,
            User = loggedUser
        });
    }

    private Task RegisterAttemptAsync(LoginCommand command, Guid? userId, bool success, CancellationToken cancellationToken)
        => attempts.AddAsync(new UserLoginAttempt
        {
            UserId = userId,
            Login = command.Login.Trim(),
            Success = success,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent
        }, cancellationToken);
}
