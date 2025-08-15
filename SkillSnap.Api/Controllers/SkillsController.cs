using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;
using System.Diagnostics;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(SkillSnapContext context, IMemoryCache cache, ILogger<SkillsController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: Public endpoint - anyone can view skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        var stopwatch = Stopwatch.StartNew();
        const string cacheKey = "skills";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out List<Skill>? skills))
        {
            _logger.LogInformation("Cache MISS for skills - fetching from database");
            
            skills = await _context.Skills
                .AsNoTracking() // Optimize for read-only queries
                .Include(s => s.PortfolioUser)
                .ToListAsync();
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, skills, TimeSpan.FromMinutes(5));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for skills - returning cached data");
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetSkills completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(skills);
    }

    // GET by ID: Public endpoint - anyone can view a specific skill
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        string cacheKey = $"skill_{id}";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out Skill? skill))
        {
            _logger.LogInformation("Cache MISS for skill {SkillId} - fetching from database", id);
            
            skill = await _context.Skills
                .AsNoTracking() // Optimize for read-only queries
                .Include(s => s.PortfolioUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                stopwatch.Stop();
                _logger.LogWarning("Skill {SkillId} not found after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
                return NotFound();
            }

            // Cache individual skill for 10 minutes
            _cache.Set(cacheKey, skill, TimeSpan.FromMinutes(10));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for skill {SkillId} - returning cached data", id);
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetSkill({SkillId}) completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            id, stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(skill);
    }

    // POST: Requires authentication - only logged-in users can create skills
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Skill>> PostSkill(Skill skill)
    {
        var stopwatch = Stopwatch.StartNew();
        
        ModelState.Remove(nameof(Skill.PortfolioUser));
        
        if(!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Optimize existence check with AsNoTracking
        var portfolioUserExists = await _context.PortfolioUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id == skill.PortfolioUserId);
        if (!portfolioUserExists)
        {
            return BadRequest($"PortfolioUser with ID {skill.PortfolioUserId} does not exist.");
        }
        
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("skills");
        _logger.LogInformation("Cache invalidated: skills list due to new skill creation");

        stopwatch.Stop();
        _logger.LogInformation("PostSkill completed in {ElapsedMs}ms - Skill {SkillId} created", 
            stopwatch.ElapsedMilliseconds, skill.Id);

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    // PUT: Requires authentication - only logged-in users can update skills
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutSkill(int id, Skill skill)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (id != skill.Id)
        {
            return BadRequest();
        }

        _context.Entry(skill).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            
            // Invalidate cache when data changes
            _cache.Remove("skills");
            _cache.Remove($"skill_{id}");
            _logger.LogInformation("Cache invalidated: skills list and skill_{SkillId} due to update", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SkillExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("PutSkill({SkillId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // DELETE: Requires Admin role - only admins can delete skills
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound();
        }

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("skills");
        _cache.Remove($"skill_{id}");
        _logger.LogInformation("Cache invalidated: skills list and skill_{SkillId} due to deletion", id);

        stopwatch.Stop();
        _logger.LogInformation("DeleteSkill({SkillId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // Helper method to check if a skill exists
    private bool SkillExists(int id)
    {
        return _context.Skills
            .AsNoTracking() // Optimize for existence check
            .Any(e => e.Id == id);
    }
}