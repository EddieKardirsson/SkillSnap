using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Api.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Url, MaxLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [ForeignKey(nameof(PortfolioUser))]
    public int PortfolioUserId { get; set; }

    // EF sets this; null-forgiving avoids nullable warnings
    public PortfolioUser PortfolioUser { get; set; } = null!;
}