using Microsoft.AspNetCore.Identity;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SeedData initialization");
        }
    }
}