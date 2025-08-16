using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<SkillSnapContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var adminRoleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                logger.LogInformation($"Admin role creation: {adminRoleResult.Succeeded}");
                
                if (!adminRoleResult.Succeeded)
                {
                    foreach (var error in adminRoleResult.Errors)
                    {
                        logger.LogError($"Error creating Admin role: {error.Description}");
                    }
                }
            }

            // Create User role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("User"))
            {
                var userRoleResult = await roleManager.CreateAsync(new IdentityRole("User"));
                logger.LogInformation($"User role creation: {userRoleResult.Succeeded}");
                
                if (!userRoleResult.Succeeded)
                {
                    foreach (var error in userRoleResult.Errors)
                    {
                        logger.LogError($"Error creating User role: {error.Description}");
                    }
                }
            }

            // Create an admin user for testing
            var adminEmail = "admin@skillsnap.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                logger.LogInformation("Creating admin user...");
                
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully");
                    
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Admin role assigned successfully");
                        
                        // Create portfolio for admin user
                        await CreateAdminPortfolio(context, adminUser, logger);
                    }
                    else
                    {
                        logger.LogError("Failed to assign Admin role:");
                        foreach (var error in roleResult.Errors)
                        {
                            logger.LogError($"Role assignment error: {error.Description}");
                        }
                    }
                }
                else
                {
                    logger.LogError("Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"User creation error: {error.Description}");
                    }
                }
            }
            else
            {
                logger.LogInformation("Admin user already exists");
                var roles = await userManager.GetRolesAsync(adminUser);
                logger.LogInformation($"Admin user roles: {string.Join(", ", roles)}");
                
                // Verify admin can login with a test password check
                var passwordCheck = await userManager.CheckPasswordAsync(adminUser, "Admin123!");
                logger.LogInformation($"Admin password verification: {passwordCheck}");
                
                // Ensure admin has a portfolio
                await CreateAdminPortfolio(context, adminUser, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SeedData initialization");
        }
    }

    private static async Task CreateAdminPortfolio(SkillSnapContext context, ApplicationUser adminUser, ILogger logger)
    {
        try
        {
            // Check if admin already has a portfolio
            var existingPortfolio = await context.PortfolioUsers
                .FirstOrDefaultAsync(p => p.ApplicationUserId == adminUser.Id);

            if (existingPortfolio == null)
            {
                logger.LogInformation("Creating portfolio for admin user...");
                
                var adminPortfolio = new PortfolioUser
                {
                    Name = "Jordan Developer",
                    Bio = "Full-stack developer passionate about learning new tech and building innovative solutions.",
                    ProfileImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop&crop=face",
                    ApplicationUserId = adminUser.Id,
                    Projects = new List<Project>
                    {
                        new Project 
                        { 
                            Title = "Task Tracker", 
                            Description = "A comprehensive task management application built with ASP.NET Core and Blazor. Features include user authentication, real-time updates, and intuitive UI.",
                            ImageUrl = "https://images.unsplash.com/photo-1611224923853-80b023f02d71?w=600&h=400&fit=crop"
                        },
                        new Project 
                        { 
                            Title = "Weather App", 
                            Description = "Modern weather forecasting application that integrates with multiple weather APIs. Provides detailed forecasts, weather maps, and location-based alerts.",
                            ImageUrl = "https://images.unsplash.com/photo-1504608524841-42fe6f032b4b?w=600&h=400&fit=crop"
                        },
                        new Project 
                        { 
                            Title = "SkillSnap Platform", 
                            Description = "A full-stack portfolio and project tracking platform built with ASP.NET Core API, Blazor WebAssembly, and SQL Server. Features authentication, caching, and modern UI.",
                            ImageUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=600&h=400&fit=crop"
                        }
                    },
                    Skills = new List<Skill>
                    {
                        new Skill { Name = "C#", Level = "Advanced" },
                        new Skill { Name = "Blazor", Level = "Advanced" },
                        new Skill { Name = "ASP.NET Core", Level = "Advanced" },
                        new Skill { Name = "Entity Framework", Level = "Intermediate" },
                        new Skill { Name = "SQL Server", Level = "Intermediate" },
                        new Skill { Name = "JavaScript", Level = "Intermediate" },
                        new Skill { Name = "Azure", Level = "Beginner" },
                        new Skill { Name = "Git", Level = "Advanced" },
                        new Skill { Name = "REST APIs", Level = "Advanced" }
                    }
                };

                context.PortfolioUsers.Add(adminPortfolio);
                await context.SaveChangesAsync();

                logger.LogInformation("Portfolio created successfully for admin user with ID: {PortfolioId}", adminPortfolio.Id);
                logger.LogInformation("Created {ProjectCount} projects and {SkillCount} skills for admin portfolio", 
                    adminPortfolio.Projects.Count, adminPortfolio.Skills.Count);
            }
            else
            {
                logger.LogInformation("Admin user already has a portfolio (ID: {PortfolioId})", existingPortfolio.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating admin portfolio");
        }
    }
}