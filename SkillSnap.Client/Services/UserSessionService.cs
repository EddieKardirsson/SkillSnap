using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillSnap.Client.Services;

public class UserSessionService
{
    private string? _userId;
    private string? _userName;
    private string? _userEmail;
    private List<string> _userRoles = new();
    private int? _currentEditingProjectId;
    private int? _currentEditingSkillId;
    private string? _token;

    // User Authentication State
    public string? UserId 
    { 
        get => _userId; 
        private set => _userId = value; 
    }

    public string? UserName 
    { 
        get => _userName; 
        private set => _userName = value; 
    }

    public string? UserEmail 
    { 
        get => _userEmail; 
        private set => _userEmail = value; 
    }

    public IReadOnlyList<string> UserRoles => _userRoles.AsReadOnly();

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(UserId);

    public bool IsAdmin => UserRoles.Contains("Admin");

    public bool IsUser => UserRoles.Contains("User");

    public string? Token => _token;

    // Current Editing State
    public int? CurrentEditingProjectId 
    { 
        get => _currentEditingProjectId; 
        set => _currentEditingProjectId = value; 
    }

    public int? CurrentEditingSkillId 
    { 
        get => _currentEditingSkillId; 
        set => _currentEditingSkillId = value; 
    }

    // Methods to manage user session from JWT token
    public void SetUserInfo(string token, string email, List<string> roles)
    {
        _token = token;
        _userEmail = email;
        _userRoles = roles ?? new List<string>();

        // Parse JWT token to extract user info
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            _userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            _userName = email; // Use email as username for now
        }
        catch (Exception)
        {
            // If token parsing fails, clear the session
            ClearUserInfo();
            return;
        }

        // Notify components that state has changed
        NotifyStateChanged();
    }

    public void ClearUserInfo()
    {
        _userId = null;
        _userName = null;
        _userEmail = null;
        _userRoles.Clear();
        _currentEditingProjectId = null;
        _currentEditingSkillId = null;
        _token = null;
        
        NotifyStateChanged();
    }

    public void SetEditingProject(int? projectId)
    {
        _currentEditingProjectId = projectId;
        NotifyStateChanged();
    }

    public void SetEditingSkill(int? skillId)
    {
        _currentEditingSkillId = skillId;
        NotifyStateChanged();
    }

    public void ClearEditingState()
    {
        _currentEditingProjectId = null;
        _currentEditingSkillId = null;
        NotifyStateChanged();
    }

    // Event to notify components when state changes
    public event Action? OnStateChanged;

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
