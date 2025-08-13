using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class SkillService : BaseApiService<Skill>, ISkillService
{
    public SkillService(HttpClient httpClient) 
        : base(httpClient, "api/skills")
    {
    }

    public async Task<List<Skill>> GetSkillsByUserIdAsync(int userId)
    {
        try
        {
            var allSkills = await GetAllAsync();
            return allSkills.Where(s => s.PortfolioUserId == userId).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting skills for user {userId}: {ex.Message}");
            return new List<Skill>();
        }
    }
}