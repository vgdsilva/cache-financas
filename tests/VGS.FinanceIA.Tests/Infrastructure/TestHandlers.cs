using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;

namespace VGS.FinanceIA.Tests.Infrastructure;

/// <summary>Monta os handlers à mão (sem DI) para os testes que usam um Postgres real.</summary>
public static class TestHandlers
{
    public const string ValidPassword = "senha-segura-1";

    public const string TestEmail = "teste@exemplo.com";

    public static string UniqueUserName() => "user-" + Guid.NewGuid().ToString("N");

    public static CreateUserHandler CreateUser(AppDbContext db, IConfiguration? configuration = null)
        => new(db, new PasswordHasher<User>(), configuration ?? new ConfigurationBuilder().Build());

    public static AuthenticateUserHandler Authenticate(AppDbContext db) => new(db, new PasswordHasher<User>());

    public static EnsureInitialAdminHandler EnsureInitialAdmin(AppDbContext db) => new(db, CreateUser(db));

    public static DatabaseSeeder Seeder(AppDbContext db, IConfiguration? configuration = null) => new(
        db,
        EnsureInitialAdmin(db),
        configuration ?? new ConfigurationBuilder().Build(),
        NullLogger<DatabaseSeeder>.Instance);

    /// <summary>Handlers de permissão compartilhando o mesmo cache, como no app real.</summary>
    public sealed class Permissions(AppDbContext db)
    {
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public GetAllowedMenusHandler GetAllowed => new(db, _cache);

        public HasMenuAccessHandler HasAccess => new(GetAllowed);

        public GrantMenuAccessHandler Grant => new(db, _cache);

        public RevokeMenuAccessHandler Revoke => new(db, _cache);

        public Task<bool> HasAsync(Guid userId, string menuCode) => HasAccess.HandleAsync(new HasMenuAccessQuery(userId, menuCode));
    }

    public static async Task<Guid> CreateUserAsync(AppDbContext db, bool isAdmin)
        => await CreateUser(db).HandleAsync(new CreateUserCommand(UniqueUserName(), ValidPassword, isAdmin, TestEmail));
}
