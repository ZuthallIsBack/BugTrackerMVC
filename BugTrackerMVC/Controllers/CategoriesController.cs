using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;

namespace BugTrackerMVC.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _ctx;
    public CategoriesController(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<IActionResult> Index()
        => View(await _ctx.Categories.OrderBy(c => c.Name).ToListAsync());

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        _ctx.Categories.Add(model);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _ctx.Categories.FindAsync(id);
        return c is null ? NotFound() : View(c);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        var c = await _ctx.Categories.FindAsync(id);
        if (c is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        c.Name = model.Name;
        c.Description = model.Description;
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _ctx.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var hasTickets = await _ctx.Tickets.AnyAsync(t => t.CategoryId == id);
        ViewBag.HasTickets = hasTickets;
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, Category model)
    {
        if (id != model.Id) return BadRequest();
        var category = await _ctx.Categories
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        if (category.Tickets.Count > 0)
        {
            _ctx.Tickets.RemoveRange(category.Tickets);
        }

        _ctx.Categories.Remove(category);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
