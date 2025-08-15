using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;

    public SkillsController(SkillSnapContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Public endpoint - anyone can view skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        const string cacheKey = "skills";
        
        if (!_cache.TryGetValue(cacheKey, out List<Skill>? skills))
        {
            skills = await _context.Skills
                .Include(s => s.PortfolioUser)
                .ToListAsync();
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, skills, TimeSpan.FromMinutes(5));
        }

        return Ok(skills);
    }

    // GET by ID: Public endpoint - anyone can view a specific skill
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        string cacheKey = $"skill_{id}";
        
        if (!_cache.TryGetValue(cacheKey, out Skill? skill))
        {
            skill = await _context.Skills
                .Include(s => s.PortfolioUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                return NotFound();
            }

            // Cache individual skill for 10 minutes
            _cache.Set(cacheKey, skill, TimeSpan.FromMinutes(10));
        }

        return Ok(skill);
    }

    // POST: Requires authentication - only logged-in users can create skills
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Skill>> PostSkill(Skill skill)
    {
        ModelState.Remove(nameof(Skill.PortfolioUser));
        
        if(!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u => u.Id == skill.PortfolioUserId);
        if (!portfolioUserExists)
        {
            return BadRequest($"PortfolioUser with ID {skill.PortfolioUserId} does not exist.");
        }
        
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("skills");

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    // PUT: Requires authentication - only logged-in users can update skills
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutSkill(int id, Skill skill)
    {
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

        return NoContent();
    }

    // DELETE: Requires Admin role - only admins can delete skills
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
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

        return NoContent();
    }

    // Helper method to check if a skill exists
    private bool SkillExists(int id)
    {
        return _context.Skills.Any(e => e.Id == id);
    }
}