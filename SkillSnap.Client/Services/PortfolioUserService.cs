using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class PortfolioUserService
{
    private readonly HttpClient _httpClient;

    public PortfolioUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get all portfolio users (public endpoint)
    /// </summary>
    public async Task<List<PortfolioUser>> GetPortfolioUsersAsync()
    {
        try
        {
            var portfolioUsers = await _httpClient.GetFromJsonAsync<List<PortfolioUser>>("api/portfoliousers");
            return portfolioUsers ?? new List<PortfolioUser>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting portfolio users: {ex.Message}");
            return new List<PortfolioUser>();
        }
    }

    /// <summary>
    /// Get specific portfolio user by ID (public endpoint)
    /// </summary>
    public async Task<PortfolioUser?> GetPortfolioUserAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PortfolioUser>($"api/portfoliousers/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting portfolio user {id}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get current authenticated user's portfolio (requires authentication)
    /// </summary>
    public async Task<PortfolioUser?> GetMyProfileAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PortfolioUser>("api/portfoliousers/my-profile");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            // User doesn't have a profile yet - this is expected
            Console.WriteLine("User doesn't have a profile yet");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting my profile: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a new portfolio for authenticated user (requires authentication)
    /// </summary>
    public async Task<PortfolioUser?> CreatePortfolioAsync(PortfolioUser newProfile)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/portfoliousers", newProfile);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PortfolioUser>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating portfolio: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating portfolio: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Update existing portfolio (requires authentication and ownership/admin)
    /// </summary>
    public async Task<bool> UpdatePortfolioAsync(int id, PortfolioUser updatedProfile)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/portfoliousers/{id}", updatedProfile);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error updating portfolio: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating portfolio: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete portfolio (requires admin role)
    /// </summary>
    public async Task<bool> DeletePortfolioAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/portfoliousers/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error deleting portfolio: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting portfolio: {ex.Message}");
            return false;
        }
    }
}
