using BugTrackerMVC.Data;
using BugTrackerMVC.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace BugTrackerMVC.Validation;

public static class TicketReferenceValidator
{
    public static async Task ValidateAsync(
        ApplicationDbContext context, TicketInput input, ModelStateDictionary modelState)
    {
        // Non-positive IDs are already rejected by the shared input annotations.
        if (input.ProjectId > 0 && !await context.Projects.AnyAsync(p => p.Id == input.ProjectId))
        {
            modelState.AddModelError(nameof(input.ProjectId), "The selected project does not exist.");
        }

        if (input.CategoryId > 0 && !await context.Categories.AnyAsync(c => c.Id == input.CategoryId))
        {
            modelState.AddModelError(nameof(input.CategoryId), "The selected category does not exist.");
        }
    }
}
