using Microsoft.AspNetCore.Identity;

namespace SkillSnap.Shared.Models;

public class ApplicationUser : IdentityUser
{
    // One-to-One relationship with PortfolioUser (no foreign key here)
    public PortfolioUser? PortfolioUser { get; set; }
}