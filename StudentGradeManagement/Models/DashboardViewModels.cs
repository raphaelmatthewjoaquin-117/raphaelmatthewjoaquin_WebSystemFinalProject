namespace StudentGradeManagement.Models;

public sealed record StudentDashboardViewModel(string StudentName, string StudentNumber, string ApiToken);

public sealed record FacultyDashboardViewModel(string FacultyName, string FacultyNumber, string ApiToken);
