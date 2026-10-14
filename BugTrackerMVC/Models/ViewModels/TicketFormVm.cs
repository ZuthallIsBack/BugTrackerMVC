using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTrackerMVC.Models.ViewModels;

public class TicketFormVm : TicketInput
{
    public TicketFormVm()
    {
        Priority = TicketPriority.Medium;
    }

    public int? Id { get; set; }

    public List<SelectListItem> Projects { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
}
