using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using BugTrackerMVC.Models;

namespace BugTrackerMVC.Models.ViewModels;

public class TicketFormVm
{
    public int? Id { get; set; }

    [Required, StringLength(120, MinimumLength = 5)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000, MinimumLength = 20)]
    public string Description { get; set; } = "";

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public List<SelectListItem> Projects { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
}
