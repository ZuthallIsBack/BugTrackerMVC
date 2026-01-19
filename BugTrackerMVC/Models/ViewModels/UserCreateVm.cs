using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTrackerMVC.Models.ViewModels;

public class UserCreateVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "User";

    public List<SelectListItem> Roles { get; set; } = new();
}
