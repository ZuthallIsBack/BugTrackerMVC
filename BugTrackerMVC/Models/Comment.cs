using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // autor (IdentityUser.Id)
    public string AuthorId { get; set; } = "";

    [Required, StringLength(1500, MinimumLength = 2)]
    public string Body { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
