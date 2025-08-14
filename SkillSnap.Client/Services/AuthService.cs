using Microsoft.JSInterop;
using SkillSnap.Shared.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkillSnap.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _token;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public event Action<bool>? AuthenticationStateChanged;

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/Auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse?.Success == true && !string.IsNullOrEmpty(authResponse.Token))
                {
                    _token = authResponse.Token;
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", _token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userEmail", authResponse.Email);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userRoles", JsonSerializer.Serialize(authResponse.Roles));
                    
                    SetAuthorizationHeader();
                    AuthenticationStateChanged?.Invoke(true);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> RegisterAsync(RegisterRequest registerRequest)
    {
        var json = JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("api/Auth/register", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _token = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userEmail");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRoles");
        
        _httpClient.DefaultRequestHeaders.Authorization = null;
        AuthenticationStateChanged?.Invoke(false);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_token != null) return true;

        try
        {
            _token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(_token))
            {
                SetAuthorizationHeader();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking authentication: {ex.Message}");
        }

        return false;
    }

    public async Task<string?> GetUserEmailAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userEmail");
        }
        catch
        {
            return null;
        }
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(_token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }
    }
}