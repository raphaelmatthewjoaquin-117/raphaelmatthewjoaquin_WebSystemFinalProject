using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudentGradeManagement.Data;
using StudentGradeManagement.Models;
using StudentGradeManagement.Services;

namespace StudentGradeManagement.Controllers.Api;

[ApiController]
[Route("api/grades")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DatabaseSeeder.FacultyRole)]
public sealed class GradesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GradeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradeResponse>>> GetGrades()
    {
        var grades = await db.StudentGrades.AsNoTracking()
            .OrderBy(grade => grade.Student.StudentNumber)
            .ThenBy(grade => grade.SubjectCode)
            .Select(GradeProjection)
            .ToListAsync();
        return Ok(grades);
    }

    [HttpGet("students")]
    [ProducesResponseType<IReadOnlyList<StudentOption>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudentOption>>> GetStudents()
    {
        var students = await db.Students.AsNoTracking()
            .Where(student => student.IsActive)
            .OrderBy(student => student.StudentNumber)
            .Select(student => new StudentOption(
                student.StudentId,
                student.StudentNumber,
                student.FirstName + " " + student.LastName))
            .ToListAsync();
        return Ok(students);
    }

    [HttpGet("subjects")]
    [ProducesResponseType<IReadOnlyList<SubjectOption>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectOption>>> GetSubjects()
    {
        var subjects = await db.StudentGrades.AsNoTracking()
            .GroupBy(grade => grade.SubjectCode)
            .Select(subjectGrades => new SubjectOption(
                subjectGrades.Key,
                subjectGrades
                    .OrderByDescending(grade => grade.DateModified ?? grade.DatePosted)
                    .Select(grade => grade.SubjectName)
                    .First()))
            .OrderBy(subject => subject.SubjectCode)
            .ToListAsync();
        return Ok(subjects);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<GradeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradeResponse>> GetGrade(int id)
    {
        var grade = await db.StudentGrades.AsNoTracking()
            .Where(item => item.GradeId == id)
            .Select(GradeProjection)
            .SingleOrDefaultAsync();
        return grade is null ? NotFound() : Ok(grade);
    }

    [HttpPost]
    [ProducesResponseType<GradeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GradeResponse>> CreateGrade(CreateGradeRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.FacultyId is not int facultyId
            || !await db.Faculty.AnyAsync(faculty => faculty.FacultyId == facultyId && faculty.IsActive))
        {
            return Forbid();
        }

        if (!await db.Students.AnyAsync(student =>
                student.StudentId == request.StudentId && student.IsActive))
        {
            ModelState.AddModelError(nameof(request.StudentId), "Select an active student.");
            return ValidationProblem(ModelState);
        }

        var grade = new StudentGrade
        {
            StudentId = request.StudentId!.Value,
            SubjectCode = request.SubjectCode!.Trim(),
            SubjectName = request.SubjectName!.Trim(),
            FacultyId = facultyId,
            Grade = decimal.ToInt32(request.Grade!.Value),
            Remarks = GradeRemarks.For(decimal.ToInt32(request.Grade.Value)),
            Semester = request.Semester!.Trim(),
            SchoolYear = request.SchoolYear!.Trim(),
            DatePosted = DateTime.UtcNow
        };
        db.StudentGrades.Add(grade);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails
            {
                Title = "A grade already exists for this student, subject, semester, and school year."
            });
        }

        var created = await db.StudentGrades.AsNoTracking()
            .Where(item => item.GradeId == grade.GradeId)
            .Select(GradeProjection)
            .SingleAsync();
        return CreatedAtAction(nameof(GetGrade), new { id = grade.GradeId }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<GradeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GradeResponse>> UpdateGrade(int id, CreateGradeRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.FacultyId is not int facultyId
            || !await db.Faculty.AnyAsync(faculty => faculty.FacultyId == facultyId && faculty.IsActive))
        {
            return Forbid();
        }

        var grade = await db.StudentGrades.SingleOrDefaultAsync(item => item.GradeId == id);
        if (grade is null)
        {
            return NotFound();
        }

        if (!await db.Students.AnyAsync(student =>
                student.StudentId == request.StudentId && student.IsActive))
        {
            ModelState.AddModelError(nameof(request.StudentId), "Select an active student.");
            return ValidationProblem(ModelState);
        }

        grade.StudentId = request.StudentId!.Value;
        grade.SubjectCode = request.SubjectCode!.Trim();
        grade.SubjectName = request.SubjectName!.Trim();
        grade.FacultyId = facultyId;
        grade.Grade = decimal.ToInt32(request.Grade!.Value);
        grade.Remarks = GradeRemarks.For(decimal.ToInt32(request.Grade.Value));
        grade.Semester = request.Semester!.Trim();
        grade.SchoolYear = request.SchoolYear!.Trim();
        grade.DateModified = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails
            {
                Title = "A grade already exists for this student, subject, semester, and school year."
            });
        }

        var updated = await db.StudentGrades.AsNoTracking()
            .Where(item => item.GradeId == grade.GradeId)
            .Select(GradeProjection)
            .SingleAsync();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGrade(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user?.FacultyId is not int facultyId
            || !await db.Faculty.AnyAsync(faculty => faculty.FacultyId == facultyId && faculty.IsActive))
        {
            return Forbid();
        }

        var grade = await db.StudentGrades.FindAsync(id);
        if (grade is null)
        {
            return NotFound();
        }

        db.StudentGrades.Remove(grade);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static readonly System.Linq.Expressions.Expression<Func<StudentGrade, GradeResponse>> GradeProjection =
        grade => new GradeResponse(
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
            grade.DateModified);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
