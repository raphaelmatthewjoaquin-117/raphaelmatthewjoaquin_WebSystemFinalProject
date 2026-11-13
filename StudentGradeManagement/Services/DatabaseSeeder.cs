using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.Data;
using StudentGradeManagement.Models;

namespace StudentGradeManagement.Services;

public static class DatabaseSeeder
{
    public const string FacultyRole = "Faculty";
    public const string StudentRole = "Student";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { FacultyRole, StudentRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(result, $"creating role {role}");
            }
        }

        await SeedAcademicRecordsAsync(db);
        await SeedAccountAsync(userManager, db, configuration, StudentRole);
        await SeedAccountAsync(userManager, db, configuration, FacultyRole);
    }

    private static async Task SeedAcademicRecordsAsync(ApplicationDbContext db)
    {
        var students = new[]
        {
            new Student { StudentNumber = "2026-0001", FirstName = "Juan", LastName = "Dela Cruz", Email = "juan.delacruz@example.com" },
            new Student { StudentNumber = "2026-0002", FirstName = "Maria", LastName = "Santos", Email = "maria.santos@example.com" },
            new Student { StudentNumber = "2026-0003", FirstName = "Pedro", LastName = "Reyes", Email = "pedro.reyes@example.com" }
        };
        foreach (var student in students)
        {
            if (!await db.Students.AnyAsync(item => item.StudentNumber == student.StudentNumber))
            {
                db.Students.Add(student);
            }
        }

        var facultyMembers = new[]
        {
            new Faculty { FacultyNumber = "FAC-001", FirstName = "Andrea", LastName = "Garcia", Email = "andrea.garcia@example.com" },
            new Faculty { FacultyNumber = "FAC-002", FirstName = "Michael", LastName = "Ramos", Email = "michael.ramos@example.com" }
        };
        foreach (var faculty in facultyMembers)
        {
            if (!await db.Faculty.AnyAsync(item => item.FacultyNumber == faculty.FacultyNumber))
            {
                db.Faculty.Add(faculty);
            }
        }

        await db.SaveChangesAsync();

        var instructor = await db.Faculty.SingleAsync(item => item.FacultyNumber == "FAC-001");
        var sampleGrades = new[]
        {
            ("2026-0001", "WEB101", "Web Systems and Technologies", 98),
            ("2026-0001", "NET101", "Networking Fundamentals", 92),
            ("2026-0002", "WEB101", "Web Systems and Technologies", 87),
            ("2026-0002", "NET101", "Networking Fundamentals", 82),
            ("2026-0003", "WEB101", "Web Systems and Technologies", 76),
            ("2026-0003", "NET101", "Networking Fundamentals", 68)
        };

        foreach (var (subjectCode, subjectName) in sampleGrades
                     .Select(grade => (grade.Item2, grade.Item3))
                     .Distinct())
        {
            if (!await db.Subjects.AnyAsync(subject => subject.SubjectCode == subjectCode))
            {
                db.Subjects.Add(new Subject
                {
                    SubjectCode = subjectCode,
                    SubjectName = subjectName
                });
            }
        }

        await db.SaveChangesAsync();

        foreach (var (studentNumber, subjectCode, subjectName, gradeValue) in sampleGrades)
        {
            var student = await db.Students.SingleAsync(item => item.StudentNumber == studentNumber);
            var exists = await db.StudentGrades.AnyAsync(grade =>
                grade.StudentId == student.StudentId
                && grade.SubjectCode == subjectCode
                && grade.Semester == "1st Semester"
                && grade.SchoolYear == "2026-2027");
            if (!exists)
            {
                db.StudentGrades.Add(new StudentGrade
                {
                    StudentId = student.StudentId,
                    SubjectCode = subjectCode,
                    SubjectName = subjectName,
                    FacultyId = instructor.FacultyId,
                    Grade = gradeValue,
                    Remarks = GradeRemarks.For(gradeValue),
                    Semester = "1st Semester",
                    SchoolYear = "2026-2027",
                    DatePosted = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAccountAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IConfiguration configuration,
        string role)
    {
        var prefix = role == StudentRole ? "Seed:Student" : "Seed:Faculty";
        var email = configuration[$"{prefix}:Email"];
        var password = configuration[$"{prefix}:Password"];
        var academicNumber = configuration[$"{prefix}:{(role == StudentRole ? "StudentNumber" : "FacultyNumber")}"];

        if (email is null && password is null && academicNumber is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(academicNumber))
        {
            throw new InvalidOperationException(
                $"Configure {prefix}:Email, {prefix}:Password, and the matching academic number together.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"creating the {role} demonstration account");
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Any(existingRole => existingRole != role))
        {
            throw new InvalidOperationException(
                $"The configured {role} email is already assigned a different role.");
        }

        if (!roles.Contains(role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(roleResult, $"assigning the {role} role");
        }

        if (role == StudentRole)
        {
            var student = await db.Students.SingleOrDefaultAsync(item => item.StudentNumber == academicNumber)
                ?? throw new InvalidOperationException($"Student number {academicNumber} does not exist.");
            if (user.FacultyId is not null || user.StudentId is not null && user.StudentId != student.StudentId)
            {
                throw new InvalidOperationException("The configured student account already has an incompatible academic mapping.");
            }

            user.StudentId = student.StudentId;
        }
        else
        {
            var faculty = await db.Faculty.SingleOrDefaultAsync(item => item.FacultyNumber == academicNumber)
                ?? throw new InvalidOperationException($"Faculty number {academicNumber} does not exist.");
            if (user.StudentId is not null || user.FacultyId is not null && user.FacultyId != faculty.FacultyId)
            {
                throw new InvalidOperationException("The configured faculty account already has an incompatible academic mapping.");
            }

            user.FacultyId = faculty.FacultyId;
        }

        var updateResult = await userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult, $"mapping the {role} account");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
        }
    }
}
