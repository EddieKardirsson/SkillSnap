using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class ProjectService : BaseApiService<Project>, IProjectService
{
    public ProjectService(HttpClient httpClient) 
        : base(httpClient, "api/projects")
    {
    }

    public async Task<List<Project>> GetProjectsByUserIdAsync(int userId)
    {
        try
        {
            var allProjects = await GetAllAsync();
            return allProjects.Where(p => p.PortfolioUserId == userId).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting projects for user {userId}: {ex.Message}");
            return new List<Project>();
        }
    }
}