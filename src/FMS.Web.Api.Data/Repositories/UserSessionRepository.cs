using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;

namespace FMS.Web.Api.Data.Repositories;

public interface IUserSessionRepository : IRepository<UserSession>;

public class UserSessionRepository(AppDbContext context) : RepositoryBase<UserSession>(context), IUserSessionRepository;
