using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;
using BugTrackerMVC.Models.ViewModels;

namespace BugTrackerMVC.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userMgr;

    public CommentsController(ApplicationDbContext ctx, UserManager<ApplicationUser> userMgr)
    {
        _ctx = ctx;
        _userMgr = userMgr;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentCreateVm vm)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets.FirstOrDefaultAsync(t => t.Id == vm.TicketId);
        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        if (!ModelState.IsValid)
            return RedirectToAction("Details", "Tickets", new { id = vm.TicketId });

        var c = new Comment
        {
            TicketId = vm.TicketId,
            AuthorId = userId ?? "",
            Body = vm.Body,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.Comments.Add(c);
        await _ctx.SaveChangesAsync();

        return RedirectToAction("Details", "Tickets", new { id = vm.TicketId });
    }
}
