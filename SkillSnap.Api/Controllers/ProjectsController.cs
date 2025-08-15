using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(SkillSnapContext context, IMemoryCache cache, ILogger<ProjectsController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: Public endpoint - anyone can view projects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        var stopwatch = Stopwatch.StartNew();
        const string cacheKey = "projects";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out List<Project>? projects))
        {
            _logger.LogInformation("Cache MISS for projects - fetching from database");
            
            projects = await _context.Projects
                .AsNoTracking() // Optimize for read-only queries
                .Include(p => p.PortfolioUser)
                .ToListAsync();
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, projects, TimeSpan.FromMinutes(5));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for projects - returning cached data");
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetProjects completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(projects);
    }

    // GET by ID: Public endpoint - anyone can view a specific project
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        string cacheKey = $"project_{id}";
        bool cacheHit = false;
        
        if (!_cache.TryGetValue(cacheKey, out Project? project))
        {
            _logger.LogInformation("Cache MISS for project {ProjectId} - fetching from database", id);
            
            project = await _context.Projects
                .AsNoTracking() // Optimize for read-only queries
                .Include(p => p.PortfolioUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                stopwatch.Stop();
                _logger.LogWarning("Project {ProjectId} not found after {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
                return NotFound();
            }

            // Cache individual project for 10 minutes
            _cache.Set(cacheKey, project, TimeSpan.FromMinutes(10));
            cacheHit = false;
        }
        else
        {
            _logger.LogInformation("Cache HIT for project {ProjectId} - returning cached data", id);
            cacheHit = true;
        }

        stopwatch.Stop();
        _logger.LogInformation("GetProject({ProjectId}) completed in {ElapsedMs}ms (Cache: {CacheStatus})", 
            id, stopwatch.ElapsedMilliseconds, cacheHit ? "HIT" : "MISS");

        return Ok(project);
    }

    // POST: Requires authentication - only logged-in users can create projects
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Project>> PostProject(Project project)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Remove PortfolioUser from ModelState validation since it's a navigation property
        ModelState.Remove(nameof(Project.PortfolioUser));
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Optimize existence check with AsNoTracking
        var portfolioUserExists = await _context.PortfolioUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id == project.PortfolioUserId);
        if (!portfolioUserExists)
        {
            return BadRequest($"PortfolioUser with ID {project.PortfolioUserId} does not exist.");
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Invalidate cache when data changes
        _cache.Remove("projects");
        _logger.LogInformation("Cache invalidated: projects list due to new project creation");

        stopwatch.Stop();
        _logger.LogInformation("PostProject completed in {ElapsedMs}ms - Project {ProjectId} created", 
            stopwatch.ElapsedMilliseconds, project.Id);

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    // PUT: Requires authentication - only logged-in users can update projects
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutProject(int id, Project project)
    {
        var stopwatch = Stopwatch.StartNew();
        
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
            _logger.LogInformation("Cache invalidated: projects list and project_{ProjectId} due to update", id);
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

        stopwatch.Stop();
        _logger.LogInformation("PutProject({ProjectId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // DELETE: Requires Admin role - only admins can delete projects
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
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
        _logger.LogInformation("Cache invalidated: projects list and project_{ProjectId} due to deletion", id);

        stopwatch.Stop();
        _logger.LogInformation("DeleteProject({ProjectId}) completed in {ElapsedMs}ms", 
            id, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    // Helper method to check if a project exists
    private bool ProjectExists(int id)
    {
        return _context.Projects
            .AsNoTracking() // Optimize for existence check
            .Any(e => e.Id == id);
    }
}