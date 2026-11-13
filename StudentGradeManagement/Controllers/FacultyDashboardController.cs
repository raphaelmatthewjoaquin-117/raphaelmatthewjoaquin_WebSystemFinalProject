using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.Data;
using StudentGradeManagement.Models;
using StudentGradeManagement.Services;

namespace StudentGradeManagement.Controllers;

[Authorize(Roles = DatabaseSeeder.FacultyRole)]
public sealed class FacultyDashboardController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IApiTokenService tokenService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.FacultyId is not int facultyId)
        {
            return Forbid();
        }

        var faculty = await db.Faculty.SingleOrDefaultAsync(item =>
            item.FacultyId == facultyId && item.IsActive);
        if (faculty is null)
        {
            return Forbid();
        }

        var token = await tokenService.CreateAsync(user);
        return View(new FacultyDashboardViewModel(
            $"{faculty.FirstName} {faculty.LastName}",
            faculty.FacultyNumber,
            token.AccessToken));
    }
}
