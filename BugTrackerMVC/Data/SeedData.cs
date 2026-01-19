using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BugTrackerMVC.Models;

namespace BugTrackerMVC.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ctx.Database.MigrateAsync();

        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminRole = "Admin";
        const string userRole = "User";
        if (!await roleMgr.RoleExistsAsync(adminRole))
            await roleMgr.CreateAsync(new IdentityRole(adminRole));
        if (!await roleMgr.RoleExistsAsync(userRole))
            await roleMgr.CreateAsync(new IdentityRole(userRole));
        var adminEmail = "admin@demo.local";
        var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };
            await userMgr.CreateAsync(admin, "Admin123!");
        }
        if (!await userMgr.IsInRoleAsync(admin, adminRole))
            await userMgr.AddToRoleAsync(admin, adminRole);

        var userEmail = "user@demo.local";
        var user = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                DisplayName = "Użytkownik"
            };
            await userMgr.CreateAsync(user, "User123!");
        }
        if (!await userMgr.IsInRoleAsync(user, userRole))
            await userMgr.AddToRoleAsync(user, userRole);
        if (!await ctx.Projects.AnyAsync())
        {
            ctx.Projects.AddRange(
                new Project { Name = "Strona WWW", Description = "Główny projekt" },
                new Project { Name = "Panel Admin", Description = "Konfiguracja" }
            );
        }

        if (!await ctx.Categories.AnyAsync())
        {
            ctx.Categories.AddRange(
                new Category { Name = "Błąd", Description = "Błędy działania" },
                new Category { Name = "Usprawnienie", Description = "Propozycje zmian" }
            );
        }

        await ctx.SaveChangesAsync();
    }
}
