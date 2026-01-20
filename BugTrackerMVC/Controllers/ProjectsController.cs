using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;

namespace BugTrackerMVC.Controllers;

[Authorize(Roles = "Admin")]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _ctx;
    public ProjectsController(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<IActionResult> Index()
        => View(await _ctx.Projects.OrderBy(p => p.Name).ToListAsync());

    public IActionResult Create() => View(new Project());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project model)
    {
        if (!ModelState.IsValid) return View(model);
        _ctx.Projects.Add(model);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _ctx.Projects.FindAsync(id);
        return p is null ? NotFound() : View(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Project model)
    {
        var p = await _ctx.Projects.FindAsync(id);
        if (p is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        p.Name = model.Name;
        p.Description = model.Description;
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var project = await _ctx.Projects.FindAsync(id);
        if (project is null) return NotFound();

        var hasTickets = await _ctx.Tickets.AnyAsync(t => t.ProjectId == id);
        ViewBag.HasTickets = hasTickets;
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, Project model)
    {
        if (id != model.Id) return BadRequest();
        var project = await _ctx.Projects
            .Include(p => p.Tickets)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        if (project.Tickets.Count > 0)
        {
            _ctx.Tickets.RemoveRange(project.Tickets);
        }

        _ctx.Projects.Remove(project);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}


