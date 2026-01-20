using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;
using BugTrackerMVC.Models.ViewModels;

namespace BugTrackerMVC.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userMgr;

    public TicketsController(ApplicationDbContext ctx, UserManager<ApplicationUser> userMgr)
    {
        _ctx = ctx;
        _userMgr = userMgr;
    }

    public async Task<IActionResult> Index(string? q, TicketStatus? status)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var query = _ctx.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .AsQueryable();

        if (!isAdmin)
            query = query.Where(t => t.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Title.Contains(q));

        if (status is not null)
            query = query.Where(t => t.Status == status);

        var data = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return View(data);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .Include(t => t.Comments)
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        ViewBag.CommentVm = new CommentCreateVm { TicketId = ticket.Id };
        return View(ticket);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new TicketFormVm();
        await FillLists(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await FillLists(vm);
            return View(vm);
        }

        var userId = _userMgr.GetUserId(User) ?? "";

        var ticket = new Ticket
        {
            Title = vm.Title,
            Description = vm.Description,
            Status = vm.Status,
            Priority = vm.Priority,
            ProjectId = vm.ProjectId,
            CategoryId = vm.CategoryId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.Tickets.Add(ticket);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        var vm = new TicketFormVm
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            ProjectId = ticket.ProjectId,
            CategoryId = ticket.CategoryId
        };

        await FillLists(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TicketFormVm vm)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        if (!ModelState.IsValid)
        {
            await FillLists(vm);
            return View(vm);
        }

        ticket.Title = vm.Title;
        ticket.Description = vm.Description;
        ticket.Status = vm.Status;
        ticket.Priority = vm.Priority;
        ticket.ProjectId = vm.ProjectId;
        ticket.CategoryId = vm.CategoryId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        return View(ticket);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deleteconfirmed(int id)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _ctx.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!isAdmin && ticket.OwnerId != userId) return Forbid();

        _ctx.Tickets.Remove(ticket);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    private async Task FillLists(TicketFormVm vm)
    {
        vm.Projects = await _ctx.Projects
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();

        vm.Categories = await _ctx.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
    }
}
