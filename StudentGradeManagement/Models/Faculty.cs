namespace StudentGradeManagement.Models;

public class Faculty
{
    public int FacultyId { get; set; }
    public required string FacultyNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<StudentGrade> Grades { get; set; } = [];
}
