using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

public class Skill
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Per spec: Level (e.g., Beginner/Intermediate/Advanced)
    [Required, MaxLength(50)]
    public string Level { get; set; } = string.Empty;

    [ForeignKey(nameof(PortfolioUser))]
    public int PortfolioUserId { get; set; }

    public PortfolioUser PortfolioUser { get; set; } = null!;
}