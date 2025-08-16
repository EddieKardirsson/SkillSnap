using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioUsersController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PortfolioUsersController> _logger;

    public PortfolioUsersController(SkillSnapContext context, IMemoryCache cache, ILogger<PortfolioUsersController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: Public endpoint - anyone can view all portfolio profiles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetPortfolioUsers()
    {
        var stopwatch = Stopwatch.StartNew();
        const string cacheKey = "portfoliousers";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out List<PortfolioUser>? portfolioUsers))
        {
            _logger.LogInformation("Cache MISS for portfolio users - fetching from database");
            
            portfolioUsers = await _context.PortfolioUsers
                .AsNoTracking()
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .ToListAsync();
            
            // Cache for 10 minutes
            _cache.Set(cacheKey, portfolioUsers, TimeSpan.FromMinutes(10));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for portfolio users - returning cached data");
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetPortfolioUsers completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(portfolioUsers);
    }

    // GET by ID: Public endpoint - anyone can view a specific portfolio
    [HttpGet("{id}")]
    public async Task<ActionResult<PortfolioUser>> GetPortfolioUser(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        string cacheKey = $"portfoliouser_{id}";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out PortfolioUser? portfolioUser))
        {
            _logger.LogInformation("Cache MISS for portfolio user {UserId} - fetching from database", id);
            
            portfolioUser = await _context.PortfolioUsers
                .AsNoTracking()
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (portfolioUser == null)
            {
                stopwatch.Stop();
                _logger.LogWarning("Portfolio user {UserId} not found after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
                return NotFound();
            }

            // Cache individual profile for 15 minutes
            _cache.Set(cacheKey, portfolioUser, TimeSpan.FromMinutes(15));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for portfolio user {UserId} - returning cached data", id);
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetPortfolioUser({UserId}) completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            id, stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(portfolioUser);
    }

    // GET current user's profile: Requires authentication
    [HttpGet("my-profile")]
    [Authorize]
    public async Task<ActionResult<PortfolioUser>> GetMyProfile()
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Unable to identify user");
        }

        _logger.LogInformation("Looking for profile for user: {UserId} ({Email})", userId, userEmail);

        // For now, we'll use email to match since PortfolioUser doesn't have ApplicationUserId reference
        // This is a design consideration - you might want to add ApplicationUserId to PortfolioUser later
        var portfolioUser = await _context.PortfolioUsers
            .Include(p => p.Projects)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Name == userEmail); // Temporary matching logic

        stopwatch.Stop();
        
        if (portfolioUser == null)
        {
            _logger.LogInformation("No profile found for user {UserId} after {ElapsedMs}ms", userId, stopwatch.ElapsedMilliseconds);
            return NotFound("No profile found for current user");
        }

        _logger.LogInformation("GetMyProfile for user {UserId} completed in {ElapsedMs}ms", userId, stopwatch.ElapsedMilliseconds);
        return Ok(portfolioUser);
    }

    // POST: Requires authentication - create new portfolio profile
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PortfolioUser>> PostPortfolioUser(PortfolioUser portfolioUser)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verify user is authenticated
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Unable to identify user");
        }

        _context.PortfolioUsers.Add(portfolioUser);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("portfoliousers");
        _logger.LogInformation("Cache invalidated: portfolio users list due to new profile creation");

        stopwatch.Stop();
        _logger.LogInformation("PostPortfolioUser completed in {ElapsedMs}ms - Profile {ProfileId} created", 
            stopwatch.ElapsedMilliseconds, portfolioUser.Id);

        return CreatedAtAction(nameof(GetPortfolioUser), new { id = portfolioUser.Id }, portfolioUser);
    }

    // PUT: Requires authentication - only users can update their own profile (or admins)
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutPortfolioUser(int id, PortfolioUser portfolioUser)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (id != portfolioUser.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user is admin or owns this profile
        var isAdmin = User.IsInRole("Admin");
        // Note: You'll need to implement ownership logic based on your design
        
        _context.Entry(portfolioUser).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            
            // Invalidate cache when data changes
            _cache.Remove("portfoliousers");
            _cache.Remove($"portfoliouser_{id}");
            _logger.LogInformation("Cache invalidated: portfolio users list and portfoliouser_{ProfileId} due to update", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PortfolioUserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("PutPortfolioUser({ProfileId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // DELETE: Requires Admin role - only admins can delete profiles
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePortfolioUser(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var portfolioUser = await _context.PortfolioUsers.FindAsync(id);
        if (portfolioUser == null)
        {
            return NotFound();
        }

        _context.PortfolioUsers.Remove(portfolioUser);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("portfoliousers");
        _cache.Remove($"portfoliouser_{id}");
        _logger.LogInformation("Cache invalidated: portfolio users list and portfoliouser_{ProfileId} due to deletion", id);

        stopwatch.Stop();
        _logger.LogInformation("DeletePortfolioUser({ProfileId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // Helper method to check if a portfolio user exists
    private bool PortfolioUserExists(int id)
    {
        return _context.PortfolioUsers
            .AsNoTracking()
            .Any(e => e.Id == id);
    }
}
