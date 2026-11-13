using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagement.Models;

public sealed class CreateGradeRequest : IValidatableObject
{
    [Required]
    [Range(1, int.MaxValue)]
    public int? StudentId { get; init; }

    [Required, StringLength(30, MinimumLength = 1)]
    public string? SubjectCode { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string? SubjectName { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal? Grade { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string? Semester { get; init; }

    [Required, StringLength(20, MinimumLength = 1)]
    public string? SchoolYear { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Grade is decimal grade && decimal.Truncate(grade) != grade)
        {
            yield return new ValidationResult("Grade must be a whole number.", [nameof(Grade)]);
        }
    }
}

public sealed record StudentOption(int StudentId, string StudentNumber, string StudentName);

public sealed record SubjectOption(string SubjectCode, string SubjectName);

public sealed record GradeResponse(
    int GradeId,
    int StudentId,
    string StudentNumber,
    string StudentName,
    string SubjectCode,
    string SubjectName,
    int FacultyId,
    string FacultyName,
    int Grade,
    string Remarks,
    string Semester,
    string SchoolYear,
    DateTime DatePosted,
    DateTime? DateModified);

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string? Email { get; init; }

    [Required]
    public string? Password { get; init; }
}

public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);
