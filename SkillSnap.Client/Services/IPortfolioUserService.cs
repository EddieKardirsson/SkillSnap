using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public interface IPortfolioUserService : IApiService<PortfolioUser>
{
    Task<PortfolioUser?> GetUserWithProjectsAndSkillsAsync(int id);
}
