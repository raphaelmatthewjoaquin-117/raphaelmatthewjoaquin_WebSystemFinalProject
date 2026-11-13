namespace StudentGradeManagement.Models;

public class Student
{
    public int StudentId { get; set; }
    public required string StudentNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<StudentGrade> Grades { get; set; } = [];
}
