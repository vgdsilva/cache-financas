using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;

namespace FMS.Web.Api.Data.Repositories;

public interface IUserLoginAttemptRepository : IRepository<UserLoginAttempt>;

public class UserLoginAttemptRepository(AppDbContext context) : RepositoryBase<UserLoginAttempt>(context), IUserLoginAttemptRepository;
