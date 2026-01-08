using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BugTrackerMVC.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(80)]
    public string? DisplayName { get; set; }
}
