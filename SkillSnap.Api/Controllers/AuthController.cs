using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// This controller handles authentication-related operations, such as user login and token generation.
// It uses ASP.NET Core Identity for user management and JWT for token-based authentication.
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // Endpoint to handle user registration.
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid registration data"
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            // Optionally assign a default role
            // await _userManager.AddToRoleAsync(user, "User");

            var token = GenerateJwtToken(user);
            
            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                Email = user.Email,
                Message = "User registered successfully"
            });
        }

        return BadRequest(new AuthResponse
        {
            Success = false,
            Message = string.Join(", ", result.Errors.Select(e => e.Description))
        });
    }

    // Endpoint to handle user login.
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid login data"
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        
        if (result.Succeeded)
        {
            var token = GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            
            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                Email = user.Email!,
                Roles = roles.ToList(),
                Message = "User logged in successfully"
            });
        }

        return Unauthorized(new AuthResponse
        {
            Success = false,
            Message = "Invalid email or password"
        });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        // Use the same path as Program.cs
        Env.Load("Keys.env");
        var jwtKey = Env.GetString("JWT_KEY") ?? 
                     throw new InvalidOperationException("JWT_KEY environment variable is not set");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles to claims if needed
        var userRoles = _userManager.GetRolesAsync(user).Result;
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(24),  // Token valid for 24 hours
            signingCredentials: credentials,
            claims: claims
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}