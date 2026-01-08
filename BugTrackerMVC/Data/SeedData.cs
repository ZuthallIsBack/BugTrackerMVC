using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugTrackerMVC.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ctx.Database.MigrateAsync();

        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        const string adminRole = "Admin";
        if (!await roleMgr.RoleExistsAsync(adminRole))
            await roleMgr.CreateAsync(new IdentityRole(adminRole));

        var adminEmail = "admin@demo.local";
        var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await userMgr.CreateAsync(admin, "Admin123!");
        }
        if (!await userMgr.IsInRoleAsync(admin, adminRole))
            await userMgr.AddToRoleAsync(admin, adminRole);

        var userEmail = "user@demo.local";
        var user = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user is null)
        {
            user = new IdentityUser { UserName = userEmail, Email = userEmail, EmailConfirmed = true };
            await userMgr.CreateAsync(user, "User123!");
        }

        if (!await ctx.Projects.AnyAsync())
        {
            ctx.Projects.AddRange(
                new Models.Project { Name = "Strona WWW", Description = "Główny projekt" },
                new Models.Project { Name = "Panel Admin", Description = "Konfiguracja" }
            );
        }

        if (!await ctx.Categories.AnyAsync())
        {
            ctx.Categories.AddRange(
                new Models.Category { Name = "Błąd", Description = "Błędy działania" },
                new Models.Category { Name = "Usprawnienie", Description = "Propozycje zmian" }
            );
        }

        await ctx.SaveChangesAsync();
    }
}
