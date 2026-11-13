using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.Data;
using StudentGradeManagement.Models;
using StudentGradeManagement.Services;

namespace StudentGradeManagement.Controllers;

[Authorize(Roles = DatabaseSeeder.StudentRole)]
public sealed class StudentDashboardController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IApiTokenService tokenService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.StudentId is not int studentId)
        {
            return Forbid();
        }

        var student = await db.Students.SingleOrDefaultAsync(item =>
            item.StudentId == studentId && item.IsActive);
        if (student is null)
        {
            return Forbid();
        }

        var token = await tokenService.CreateAsync(user);
        return View(new StudentDashboardViewModel(
            $"{student.FirstName} {student.LastName}",
            student.StudentNumber,
            token.AccessToken));
    }
}
