using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkillSnap.Shared.Models;

public class PortfolioUser
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Bio { get; set; } = string.Empty;

    [Url, MaxLength(2048)]
    public string ProfileImageUrl { get; set; } = string.Empty;

    // One-to-One relationship with ApplicationUser
    [ForeignKey(nameof(ApplicationUser))]
    public string? ApplicationUserId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationUser? ApplicationUser { get; set; }

    // One-to-Many relationships
    public List<Project> Projects { get; set; } = new();
    public List<Skill> Skills { get; set; } = new();
}