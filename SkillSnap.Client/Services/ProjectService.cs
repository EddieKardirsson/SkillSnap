using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class ProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        try
        {
            var projects = await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
            return projects ?? new List<Project>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting projects: {ex.Message}");
            return new List<Project>();
        }
    }

    public async Task<Project?> AddProjectAsync(Project newProject)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/projects", newProject);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Project>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding project: {ex.Message}");
            return null;
        }
    }

    public async Task<Project?> GetProjectAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Project>($"api/projects/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting project {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateProjectAsync(int id, Project updatedProject)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/projects/{id}", updatedProject);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating project: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/projects/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting project: {ex.Message}");
            return false;
        }
    }
}