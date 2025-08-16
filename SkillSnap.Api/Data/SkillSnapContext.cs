using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Data;

public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public SkillSnapContext(DbContextOptions<SkillSnapContext> options) : base(options) { }

    public DbSet<PortfolioUser> PortfolioUsers { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Skill> Skills { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure One-to-One relationship between ApplicationUser and PortfolioUser
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(au => au.PortfolioUser)
            .WithOne(pu => pu.ApplicationUser)
            .HasForeignKey<PortfolioUser>(pu => pu.ApplicationUserId)
            .IsRequired(true); // Each PortfolioUser must have an ApplicationUser

        // PortfolioUser (1) -> Project (many)
        modelBuilder.Entity<Project>()
            .HasOne(p => p.PortfolioUser)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.PortfolioUserId)
            .OnDelete(DeleteBehavior.Cascade); 

        // PortfolioUser (1) -> Skill (many)
        modelBuilder.Entity<Skill>()
            .HasOne(s => s.PortfolioUser)
            .WithMany(u => u.Skills)
            .HasForeignKey(s => s.PortfolioUserId)
            .OnDelete(DeleteBehavior.Cascade); 
    }
}