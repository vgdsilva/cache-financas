using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FMS.Web.Api.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    /// <summary>Busca por login ou e-mail (sem diferenciar maiúsculas).</summary>
    Task<User?> GetByLoginOrEmailAsync(string loginOrEmail, CancellationToken cancellationToken = default);
}

public class UserRepository(AppDbContext context) : RepositoryBase<User>(context), IUserRepository
{
    public async Task<User?> GetByLoginOrEmailAsync(string loginOrEmail, CancellationToken cancellationToken = default)
    {
        var value = loginOrEmail.Trim().ToLower();
        return await DbSet.FirstOrDefaultAsync(
            x => x.Login.ToLower() == value || x.Email.ToLower() == value, cancellationToken);
    }
}
