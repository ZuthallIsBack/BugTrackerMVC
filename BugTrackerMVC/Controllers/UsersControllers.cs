using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BugTrackerMVC.Models;
using BugTrackerMVC.Models.ViewModels;

namespace BugTrackerMVC.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly RoleManager<IdentityRole> _roleMgr;

    public UsersController(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr)
    {
        _userMgr = userMgr;
        _roleMgr = roleMgr;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userMgr.Users.ToList();
        var list = new List<UserListItemVm>();
        foreach (var user in users)
        {
            var roles = await _userMgr.GetRolesAsync(user);
            list.Add(new UserListItemVm
            {
                Id = user.Id,
                Email = user.Email ?? "",
                DisplayName = user.DisplayName,
                Roles = string.Join(", ", roles)
            });
        }
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new UserCreateVm();
        await FillRoles(vm.Roles);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await FillRoles(vm.Roles);
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            EmailConfirmed = true,
            DisplayName = vm.DisplayName
        };

        var result = await _userMgr.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            await FillRoles(vm.Roles);
            return View(vm);
        }

        await EnsureRoleExists(vm.Role);
        await _userMgr.AddToRoleAsync(user, vm.Role);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userMgr.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await _userMgr.GetRolesAsync(user);
        var vm = new UserEditVm
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName,
            Role = roles.FirstOrDefault() ?? "User"
        };

        await FillRoles(vm.Roles);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditVm vm)
    {
        if (id != vm.Id) return BadRequest();
        var user = await _userMgr.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await FillRoles(vm.Roles);
            return View(vm);
        }

        user.Email = vm.Email;
        user.UserName = vm.Email;
        user.DisplayName = vm.DisplayName;
        var update = await _userMgr.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await FillRoles(vm.Roles);
            return View(vm);
        }

        var existingRoles = await _userMgr.GetRolesAsync(user);
        if (!existingRoles.Contains(vm.Role))
        {
            await _userMgr.RemoveFromRolesAsync(user, existingRoles);
            await EnsureRoleExists(vm.Role);
            await _userMgr.AddToRoleAsync(user, vm.Role);
        }

        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
            var reset = await _userMgr.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!reset.Succeeded)
            {
                foreach (var error in reset.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                await FillRoles(vm.Roles);
                return View(vm);
            }
        }

        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userMgr.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await _userMgr.GetRolesAsync(user);
        var vm = new UserListItemVm
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName,
            Roles = string.Join(", ", roles)
        };

        ViewBag.IsSelf = _userMgr.GetUserId(User) == user.Id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, UserListItemVm vm)
    {
        if (id != vm.Id) return BadRequest();
        var user = await _userMgr.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (_userMgr.GetUserId(User) == user.Id)
        {
            ModelState.AddModelError(string.Empty, "Nie możesz usunąć własnego konta administratora.");
            var roles = await _userMgr.GetRolesAsync(user);
            vm.Email = user.Email ?? "";
            vm.DisplayName = user.DisplayName;
            vm.Roles = string.Join(", ", roles);
            ViewBag.IsSelf = true;
            return View(vm);
        }

        var result = await _userMgr.DeleteAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            var roles = await _userMgr.GetRolesAsync(user);
            vm.Email = user.Email ?? "";
            vm.DisplayName = user.DisplayName;
            vm.Roles = string.Join(", ", roles);
            ViewBag.IsSelf = false;
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }


    private async Task FillRoles(List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> roles)
    {
        await EnsureRoleExists("Admin");
        await EnsureRoleExists("User");

        roles.Clear();
        roles.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("User", "User"));
        roles.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("Admin", "Admin"));
    }

    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleMgr.RoleExistsAsync(role))
            await _roleMgr.CreateAsync(new IdentityRole(role));
    }
}
