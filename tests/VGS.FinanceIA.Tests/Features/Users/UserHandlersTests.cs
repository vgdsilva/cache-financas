using Microsoft.Extensions.Configuration;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Tests.Features.Users;

public class UserNamesTests
{
    [Fact]
    public void Normalize_TrimsAndLowercases()
    {
        Assert.Equal("vinicius", UserNames.Normalize("  ViNiCiUs "));
    }
}

/// <summary>A validação do tamanho da senha acontece antes de qualquer acesso ao banco, então roda sem Docker.</summary>
public class PasswordLengthTests
{
    private static CreateUserHandler Handler(string? minLength)
    {
        var settings = new Dictionary<string, string?>();
        if (minLength is not null)
        {
            settings[UserNames.MinPasswordLengthSetting] = minLength;
        }

        var db = new VGS.FinanceIA.Data.AppDbContext(
            new DbContextOptionsBuilder<VGS.FinanceIA.Data.AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

        return TestHandlers.CreateUser(db, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    [Theory]
    [InlineData(null, "1234567")]
    [InlineData("3", "12")]
    [InlineData("0", "")] // mínimo nunca fica abaixo de 1
    public async Task TooShortPassword_IsRejected(string? minLength, string password)
        => await Assert.ThrowsAsync<ArgumentException>(
            () => Handler(minLength).HandleAsync(new CreateUserCommand("admin", password, IsAdmin: true)));

    [Fact]
    public async Task DefaultMinimum_Is8()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => Handler(null).HandleAsync(new CreateUserCommand("admin", "123", IsAdmin: true)));

        Assert.Contains("8", error.Message);
    }
}

public class UserProfileTests
{
    [Theory]
    [InlineData(true, "Administrador")]
    [InlineData(false, "Usuário")]
    public void RoleLabel_FollowsIsAdmin(bool isAdmin, string expected)
        => Assert.Equal(expected, new UserProfile("x", isAdmin).RoleLabel);
}

public class UserHandlersTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [DockerFact]
    public async Task GetUserProfile_ReturnsNameAndRole_AndNullForDeletedUser()
    {
        await using var db = postgres.CreateDbContext();
        var adminId = await TestHandlers.CreateUserAsync(db, isAdmin: true);
        var handler = new GetUserProfileHandler(db);

        var profile = await handler.HandleAsync(new GetUserProfileQuery(adminId));
        Assert.True(profile?.IsAdmin);

        db.Users.Remove(await db.Users.SingleAsync(u => u.Id == adminId));
        await db.SaveChangesAsync();

        Assert.Null(await handler.HandleAsync(new GetUserProfileQuery(adminId)));
        Assert.Null(await handler.HandleAsync(new GetUserProfileQuery(Guid.NewGuid())));
    }

    [DockerFact]
    public async Task CreateUser_NormalizesName_AndHashesPassword()
    {
        await using var db = postgres.CreateDbContext();
        var name = TestHandlers.UniqueUserName();

        var id = await TestHandlers.CreateUser(db)
            .HandleAsync(new CreateUserCommand("  " + name.ToUpperInvariant() + " ", TestHandlers.ValidPassword, IsAdmin: false));

        var user = await db.Users.SingleAsync(u => u.Id == id);
        Assert.Equal(name, user.UserName);
        Assert.NotEqual(TestHandlers.ValidPassword, user.PasswordHash);
    }

    [DockerFact]
    public async Task CreateUser_RejectsDuplicateName_AndShortPassword()
    {
        await using var db = postgres.CreateDbContext();
        var handler = TestHandlers.CreateUser(db);
        var name = TestHandlers.UniqueUserName();
        await handler.HandleAsync(new CreateUserCommand(name, TestHandlers.ValidPassword, IsAdmin: false));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new CreateUserCommand(name, "outra-senha-2", IsAdmin: false)));
        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(new CreateUserCommand(TestHandlers.UniqueUserName(), "curta", IsAdmin: false)));
    }

    [DockerFact]
    public async Task Authenticate_AcceptsCorrectPassword_RejectsOthers()
    {
        await using var db = postgres.CreateDbContext();
        var name = TestHandlers.UniqueUserName();
        await TestHandlers.CreateUser(db).HandleAsync(new CreateUserCommand(name, TestHandlers.ValidPassword, IsAdmin: false));
        var authenticate = TestHandlers.Authenticate(db);

        var ok = await authenticate.HandleAsync(new AuthenticateUserCommand(name.ToUpperInvariant(), TestHandlers.ValidPassword));

        Assert.Equal(name, ok?.UserName);
        Assert.Null(await authenticate.HandleAsync(new AuthenticateUserCommand(name, "senha-errada-1")));
        Assert.Null(await authenticate.HandleAsync(new AuthenticateUserCommand("nao-existe", TestHandlers.ValidPassword)));
    }

    [DockerFact]
    public async Task EnsureInitialAdmin_OnlyCreatesWhenThereAreNoUsers()
    {
        await using var db = postgres.CreateDbContext();
        var ensure = TestHandlers.EnsureInitialAdmin(db);

        // Já existe usuário (de outros testes ou desta chamada): a segunda chamada nunca cria.
        await ensure.HandleAsync(new EnsureInitialAdminCommand(TestHandlers.UniqueUserName(), TestHandlers.ValidPassword));
        var created = await ensure.HandleAsync(new EnsureInitialAdminCommand(TestHandlers.UniqueUserName(), TestHandlers.ValidPassword));

        Assert.False(created);
    }
}
