using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<SkillSnapContext>(options =>
            options.UseSqlite("Data Source=./Data/skillsnap.db"));
        
        // Add ASP.NET Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<SkillSnapContext>()
            .AddDefaultTokenProviders();

        // Configure JWT Authentication
        Env.Load("Keys.env"); // Load environment variables from .env file if present
        var jwtKey = Env.GetString("JWT_KEY")
            ?? throw new InvalidOperationException("JWT_KEY environment variable is not set");
        
        var key = Encoding.UTF8.GetBytes(jwtKey);
        
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        
        // Add services to the container.
        builder.Services.AddAuthorization();

        // Add Memory Cache for performance optimization
        builder.Services.AddMemoryCache();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Fix circular reference issues.
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // CORS: allow the client origin(s) from configuration
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ClientPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        // Development: Clear database and reset identity columns, uncomment when needed
        /*if (app.Environment.IsDevelopment())
        {
            await ClearAndResetDatabase(app.Services);
        }*/

        // Initialize roles and admin user after clearing database
        await SeedData.InitializeAsync(app.Services);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // Authentication must come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseCors("ClientPolicy");
        app.MapControllers();

        app.Run();
    }

    private static async Task ClearAndResetDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SkillSnapContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Clearing database and resetting identity columns...");

            // Clear all data (in correct order to avoid foreign key constraints)
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Skills");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Projects");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM PortfolioUsers");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUserRoles");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetRoles");

            // Reset SQLite sequence numbers (equivalent to IDENTITY reset)
            await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='PortfolioUsers'");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='Projects'");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='Skills'");

            // Note: AspNetUsers and AspNetRoles use GUIDs, so no sequence reset needed

            logger.LogInformation("Database cleared and identity columns reset successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing database");
            throw;
        }
    }
}