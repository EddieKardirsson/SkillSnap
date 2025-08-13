using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class SkillService
{
    private readonly HttpClient _httpClient;

    public SkillService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        try
        {
            var skills = await _httpClient.GetFromJsonAsync<List<Skill>>("api/skills");
            return skills ?? new List<Skill>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting skills: {ex.Message}");
            return new List<Skill>();
        }
    }

    public async Task<Skill?> AddSkillAsync(Skill newSkill)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/skills", newSkill);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Skill>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding skill: {ex.Message}");
            return null;
        }
    }
}