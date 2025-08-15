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

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public bool IsAdmin => UserRoles.Contains("Admin");

    public bool IsUser => UserRoles.Contains("User");

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

    // Methods to manage user session
    public void SetUserInfo(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _userName = user.FindFirst(ClaimTypes.Name)?.Value;
            _userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            
            // Extract roles
            _userRoles.Clear();
            var roleClaims = user.FindAll(ClaimTypes.Role);
            foreach (var roleClaim in roleClaims)
            {
                _userRoles.Add(roleClaim.Value);
            }
        }
        else
        {
            ClearUserInfo();
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
