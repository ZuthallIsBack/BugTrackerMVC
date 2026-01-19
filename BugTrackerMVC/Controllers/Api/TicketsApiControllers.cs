using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;

namespace BugTrackerMVC.Controllers.Api;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsApiController : ControllerBase
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userMgr;

    public TicketsApiController(ApplicationDbContext ctx, UserManager<ApplicationUser> userMgr)
    {
        _ctx = ctx;
        _userMgr = userMgr;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var q = _ctx.Tickets.Include(t => t.Project).Include(t => t.Category).AsQueryable();
        if (!isAdmin) q = q.Where(t => t.OwnerId == userId);

        var data = await q.OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.CreatedAt,
                Project = t.Project!.Name,
                Category = t.Category!.Name
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var t = await _ctx.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!isAdmin && t.OwnerId != userId) return Forbid();

        return Ok(new
        {
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.ProjectId,
            t.CategoryId,
            t.CreatedAt,
            t.UpdatedAt
        });
    }

    public record TicketCreateDto(string Title, string Description, TicketStatus Status, TicketPriority Priority, int ProjectId, int CategoryId);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TicketCreateDto dto)
    {
        if (dto.Title is null || dto.Title.Length < 5) return BadRequest("Title");
        if (dto.Description is null || dto.Description.Length < 20) return BadRequest("Description");

        var userId = _userMgr.GetUserId(User) ?? "";

        var t = new Ticket
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            ProjectId = dto.ProjectId,
            CategoryId = dto.CategoryId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.Tickets.Add(t);
        await _ctx.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { t.Id });
    }

    public record TicketUpdateDto(string Title, string Description, TicketStatus Status, TicketPriority Priority, int ProjectId, int CategoryId);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TicketUpdateDto dto)
    {
        var userId = _userMgr.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var t = await _ctx.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!isAdmin && t.OwnerId != userId) return Forbid();

        t.Title = dto.Title;
        t.Description = dto.Description;
        t.Status = dto.Status;
        t.Priority = dto.Priority;
        t.ProjectId = dto.ProjectId;
        t.CategoryId = dto.CategoryId;
        t.UpdatedAt = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _ctx.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        _ctx.Tickets.Remove(t);
        await _ctx.SaveChangesAsync();
        return NoContent();
    }
}
