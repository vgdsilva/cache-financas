using FMS.Communication.Features.Auth.Commands;
using FMS.Communication.Features.Auth.Dtos;
using FMS.Communication.Features.Auth.Queries;
using FMS.Web.Api.Bussiness.Rules.Abstractions;
using FMS.Web.Api.Bussiness.Rules.Features.Auth.Commands;
using FMS.Web.Api.Bussiness.Rules.Features.Auth.Queries;
using FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FMS.Web.Api.Bussiness.Rules;

public static class BusinessRulesServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessRules(this IServiceCollection services, JwtOptions jwtOptions)
    {
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ILoggedUserBuilder, LoggedUserBuilder>();

        services.AddScoped<ICommandHandler<LoginCommand, LoginResultDto>, LoginCommandHandler>();
        services.AddScoped<IQueryHandler<GetLoggedUserQuery, LoggedUserDto>, GetLoggedUserQueryHandler>();

        return services;
    }
}
