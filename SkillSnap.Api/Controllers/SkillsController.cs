using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext _context;

    public SkillsController(SkillSnapContext context)
    {
        _context = context;
    }

    // GET: Public endpoint - anyone can view skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        return await _context.Skills
            .Include(s => s.PortfolioUser)
            .ToListAsync();
    }

    // GET by ID: Public endpoint - anyone can view a specific skill
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var skill = await _context.Skills
            .Include(s => s.PortfolioUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null)
        {
            return NotFound();
        }

        return skill;
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

        return NoContent();
    }

    // Helper method to check if a skill exists
    private bool SkillExists(int id)
    {
        return _context.Skills.Any(e => e.Id == id);
    }
}