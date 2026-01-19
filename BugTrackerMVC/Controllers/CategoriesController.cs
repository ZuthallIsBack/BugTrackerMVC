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
}
