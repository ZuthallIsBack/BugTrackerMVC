using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models;

// Shared by MVC forms and both API write endpoints.
public class TicketInput
{
    [Required, StringLength(TicketValidationRules.TitleMaxLength,
        MinimumLength = TicketValidationRules.TitleMinLength)]
    public string Title { get; set; } = "";

    [Required, StringLength(TicketValidationRules.DescriptionMaxLength,
        MinimumLength = TicketValidationRules.DescriptionMinLength)]
    public string Description { get; set; } = "";

    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; set; }

    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ProjectId must be a positive integer.")]
    public int ProjectId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer.")]
    public int CategoryId { get; set; }
}
