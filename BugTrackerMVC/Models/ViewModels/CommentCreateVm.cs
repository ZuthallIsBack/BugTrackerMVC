using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models.ViewModels;

public class CommentCreateVm
{
    [Required]
    public int TicketId { get; set; }

    [Required, StringLength(1500, MinimumLength = 2)]
    public string Body { get; set; } = "";
}
