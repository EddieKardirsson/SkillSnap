using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;

    public ProjectsController(SkillSnapContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Public endpoint - anyone can view projects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        const string cacheKey = "projects";
        
        if (!_cache.TryGetValue(cacheKey, out List<Project>? projects))
        {
            projects = await _context.Projects
                .Include(p => p.PortfolioUser)
                .ToListAsync();
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, projects, TimeSpan.FromMinutes(5));
        }

        return Ok(projects);
    }

    // GET by ID: Public endpoint - anyone can view a specific project
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        string cacheKey = $"project_{id}";
        
        if (!_cache.TryGetValue(cacheKey, out Project? project))
        {
            project = await _context.Projects
                .Include(p => p.PortfolioUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Cache individual project for 10 minutes
            _cache.Set(cacheKey, project, TimeSpan.FromMinutes(10));
        }

        return Ok(project);
    }

    // POST: Requires authentication - only logged-in users can create projects
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Project>> PostProject(Project project)
    {
        // Remove PortfolioUser from ModelState validation since it's a navigation property
        ModelState.Remove(nameof(Project.PortfolioUser));
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Verify the PortfolioUser exists
        var portfolioUserExists = await _context.PortfolioUsers.AnyAsync(u => u.Id == project.PortfolioUserId);
        if (!portfolioUserExists)
        {
            return BadRequest($"PortfolioUser with ID {project.PortfolioUserId} does not exist.");
        }
    
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    
        // Invalidate cache when data changes
        _cache.Remove("projects");
    
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    // PUT: Requires authentication - only logged-in users can update projects
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutProject(int id, Project project)
    {
        if (id != project.Id)
        {
            return BadRequest();
        }

        _context.Entry(project).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            
            // Invalidate cache when data changes
            _cache.Remove("projects");
            _cache.Remove($"project_{id}");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProjectExists(id))
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

    // DELETE: Requires Admin role - only admins can delete projects
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("projects");
        _cache.Remove($"project_{id}");

        return NoContent();
    }

    // Helper method to check if a project exists
    private bool ProjectExists(int id)
    {
        return _context.Projects.Any(e => e.Id == id);
    }
}