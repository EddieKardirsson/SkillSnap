using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public interface IProjectService : IApiService<Project>
{
    Task<List<Project>> GetProjectsByUserIdAsync(int userId);
}
