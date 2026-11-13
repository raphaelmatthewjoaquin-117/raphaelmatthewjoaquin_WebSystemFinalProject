using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using StudentGradeManagement.Models;

namespace StudentGradeManagement.Tests;

public class GradeRemarksTests
{
    [Fact]
    public void For_UsesEveryGradeBandBoundary()
    {
        Assert.Equal("BEP/FAILED", GradeRemarks.For(0));
        Assert.Equal("BEP/FAILED", GradeRemarks.For(74));
        Assert.Equal("EP", GradeRemarks.For(75));
        Assert.Equal("EP", GradeRemarks.For(79));
        Assert.Equal("GP", GradeRemarks.For(80));
        Assert.Equal("GP", GradeRemarks.For(84));
        Assert.Equal("SAP", GradeRemarks.For(85));
        Assert.Equal("SAP", GradeRemarks.For(89));
        Assert.Equal("SP", GradeRemarks.For(90));
        Assert.Equal("SP", GradeRemarks.For(94));
        Assert.Equal("CMS", GradeRemarks.For(95));
        Assert.Equal("CMS", GradeRemarks.For(100));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void For_RejectsGradesOutsideRange(int grade)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GradeRemarks.For(grade));
    }

    [Fact]
    public void RequestValidation_RejectsMissingGrade()
    {
        var request = new CreateGradeRequest
        {
            StudentId = 1,
            SubjectCode = "WEB101",
            SubjectName = "Web Systems",
            Semester = "1st Semester",
            SchoolYear = "2026-2027"
        };
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RequestValidation_RejectsGradesOutsideRange(int grade)
    {
        var request = new CreateGradeRequest
        {
            StudentId = 1,
            SubjectCode = "WEB101",
            SubjectName = "Web Systems",
            Grade = grade,
            Semester = "1st Semester",
            SchoolYear = "2026-2027"
        };
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true));
    }

    [Fact]
    public void JsonBinding_ValidatesFractionalGradeAsInvalid()
    {
        const string request = """
            {"studentId":1,"subjectCode":"WEB101","subjectName":"Web Systems","grade":84.5,"semester":"1st Semester","schoolYear":"2026-2027"}
            """;

        var model = JsonSerializer.Deserialize<CreateGradeRequest>(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(model);
        Assert.Equal(84.5m, model.Grade);
        Assert.False(Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            new List<ValidationResult>(),
            validateAllProperties: true));
    }
}
