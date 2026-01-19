using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTrackerMVC.Models.ViewModels;

public class UserEditVm
{
    [Required]
    public string Id { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = "";

    [StringLength(100, MinimumLength = 8)]
    public string? NewPassword { get; set; }

    [Required]
    public string Role { get; set; } = "User";

    public List<SelectListItem> Roles { get; set; } = new();
}
