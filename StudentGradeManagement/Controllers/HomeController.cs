using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.Models;

namespace StudentGradeManagement.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.IsInRole("Faculty"))
        {
            return RedirectToAction("Index", "FacultyDashboard");
        }

        if (User.IsInRole("Student"))
        {
            return RedirectToAction("Index", "StudentDashboard");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
