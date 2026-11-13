using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.Data;
using StudentGradeManagement.Models;
using StudentGradeManagement.Services;

namespace StudentGradeManagement.Controllers.Api;

[ApiController]
[Route("api/student/grades")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DatabaseSeeder.StudentRole)]
public sealed class StudentGradesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GradeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<GradeResponse>>> GetMyGrades()
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.StudentId is not int studentId
            || !await db.Students.AnyAsync(student => student.StudentId == studentId && student.IsActive))
        {
            return Forbid();
        }

        var grades = await db.StudentGrades.AsNoTracking()
            .Where(grade => grade.StudentId == studentId)
            .OrderBy(grade => grade.SubjectCode)
            .Select(grade => new GradeResponse(
                grade.GradeId,
                grade.StudentId,
                grade.Student.StudentNumber,
                grade.Student.FirstName + " " + grade.Student.LastName,
                grade.SubjectCode,
                grade.SubjectName,
                grade.FacultyId,
                grade.Faculty.FirstName + " " + grade.Faculty.LastName,
                grade.Grade,
                grade.Remarks,
                grade.Semester,
                grade.SchoolYear,
                grade.DatePosted,
                grade.DateModified))
            .ToListAsync();
        return Ok(grades);
    }
}
