using System.Net.Http.Json;
using System.Text.Json;

namespace SkillSnap.Client.Services;

public abstract class BaseApiService<T> : IApiService<T>
{
    protected readonly HttpClient _httpClient;
    protected readonly string _endpoint;

    protected BaseApiService(HttpClient httpClient, string endpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_endpoint);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all {typeof(T).Name}: {ex.Message}");
            return new List<T>();
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                return default(T);
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting {typeof(T).Name} by id {id}: {ex.Message}");
            return default(T);
        }
    }

    public virtual async Task<T?> CreateAsync(T entity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_endpoint, entity);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating {typeof(T).Name}: {ex.Message}");
            return default(T);
        }
    }

    public virtual async Task<T?> UpdateAsync(int id, T entity)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_endpoint}/{id}", entity);
            
            if (response.IsSuccessStatusCode)
            {
                // For PUT, some APIs return 204 No Content, others return the updated entity
                if (response.Content.Headers.ContentLength > 0)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<T>(json, options);
                }
                return entity; // Return the entity we sent if no content returned
            }
            
            return default(T);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating {typeof(T).Name} with id {id}: {ex.Message}");
            return default(T);
        }
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting {typeof(T).Name} with id {id}: {ex.Message}");
            return false;
        }
    }
}
