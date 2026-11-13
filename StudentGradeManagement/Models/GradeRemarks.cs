namespace StudentGradeManagement.Models;

public static class GradeRemarks
{
    public static string For(int grade)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(grade, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(grade, 100);

        return grade switch
        {
            >= 95 => "CMS",
            >= 90 => "SP",
            >= 85 => "SAP",
            >= 80 => "GP",
            >= 75 => "EP",
            _ => "BEP/FAILED"
        };
    }
}
