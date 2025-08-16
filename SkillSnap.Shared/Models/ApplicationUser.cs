using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

public class ApplicationUser : IdentityUser
{
    // One-to-One relationship with PortfolioUser
    [ForeignKey(nameof(PortfolioUser))]
    public int? PortfolioUserId { get; set; }
    
    public PortfolioUser? PortfolioUser { get; set; }
}