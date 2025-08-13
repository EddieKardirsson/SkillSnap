using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public interface ISkillService : IApiService<Skill>
{
    Task<List<Skill>> GetSkillsByUserIdAsync(int userId);
}