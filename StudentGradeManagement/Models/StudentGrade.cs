namespace StudentGradeManagement.Models;

public class StudentGrade
{
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public required string SubjectCode { get; set; }
    public required string SubjectName { get; set; }
    public int FacultyId { get; set; }
    public Faculty Faculty { get; set; } = null!;
    public int Grade { get; set; }
    public required string Remarks { get; set; }
    public required string Semester { get; set; }
    public required string SchoolYear { get; set; }
    public DateTime DatePosted { get; set; }
    public DateTime? DateModified { get; set; }
}
