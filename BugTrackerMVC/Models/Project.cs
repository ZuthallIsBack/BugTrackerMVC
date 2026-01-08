using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models;

public class Project
{
    public int Id { get; set; }

    [Required, StringLength(120, MinimumLength = 3)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}
