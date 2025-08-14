namespace SkillSnap.Shared.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}