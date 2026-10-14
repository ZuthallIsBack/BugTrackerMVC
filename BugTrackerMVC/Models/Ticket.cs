using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models;

public enum TicketStatus { New = 0, InProgress = 1, Resolved = 2, Closed = 3 }
public enum TicketPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }

public class Ticket
{
    public int Id { get; set; }

    [Required, StringLength(TicketValidationRules.TitleMaxLength,
        MinimumLength = TicketValidationRules.TitleMinLength)]
    public string Title { get; set; } = "";

    [Required, StringLength(TicketValidationRules.DescriptionMaxLength,
        MinimumLength = TicketValidationRules.DescriptionMinLength)]
    public string Description { get; set; } = "";

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // właściciel (IdentityUser.Id)
    public string OwnerId { get; set; } = "";

    public List<Comment> Comments { get; set; } = new();
}
