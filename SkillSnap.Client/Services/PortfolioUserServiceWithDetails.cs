using SkillSnap.Shared.Models;
using System.Text.Json;

namespace SkillSnap.Client.Services;

public class PortfolioUserService : BaseApiService<PortfolioUser>, IPortfolioUserService
{
    public PortfolioUserService(HttpClient httpClient) 
        : base(httpClient, "api/portfoliousers")
    {
    }

    public async Task<PortfolioUser?> GetUserWithProjectsAndSkillsAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/{id}/with-details");
            
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to regular get if with-details doesn't exist yet
                return await GetByIdAsync(id);
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<PortfolioUser>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user with details: {ex.Message}");
            return await GetByIdAsync(id); // Fallback
        }
    }
}
