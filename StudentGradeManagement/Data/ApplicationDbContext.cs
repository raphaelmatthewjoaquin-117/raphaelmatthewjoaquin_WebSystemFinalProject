using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.Models;

namespace StudentGradeManagement.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Faculty> Faculty => Set<Faculty>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudentGrade> StudentGrades => Set<StudentGrade>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Student>(entity =>
        {
            entity.HasIndex(student => student.StudentNumber).IsUnique();
            entity.HasIndex(student => student.Email).IsUnique();
            entity.Property(student => student.StudentNumber).HasMaxLength(30).IsRequired();
            entity.Property(student => student.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(student => student.LastName).HasMaxLength(100).IsRequired();
            entity.Property(student => student.Email).HasMaxLength(256).IsRequired();
        });

        builder.Entity<Faculty>(entity =>
        {
            entity.HasIndex(faculty => faculty.FacultyNumber).IsUnique();
            entity.HasIndex(faculty => faculty.Email).IsUnique();
            entity.Property(faculty => faculty.FacultyNumber).HasMaxLength(30).IsRequired();
            entity.Property(faculty => faculty.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(faculty => faculty.LastName).HasMaxLength(100).IsRequired();
            entity.Property(faculty => faculty.Email).HasMaxLength(256).IsRequired();
        });

        builder.Entity<Subject>(entity =>
        {
            entity.HasKey(subject => subject.SubjectCode);
            entity.Property(subject => subject.SubjectCode).HasMaxLength(30).IsRequired();
            entity.Property(subject => subject.SubjectName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<StudentGrade>(entity =>
        {
            entity.HasKey(grade => grade.GradeId);
            entity.HasIndex(grade => new
            {
                grade.StudentId,
                grade.SubjectCode,
                grade.Semester,
                grade.SchoolYear
            }).IsUnique();
            entity.HasIndex(grade => grade.FacultyId);
            entity.Property(grade => grade.SubjectCode).HasMaxLength(30).IsRequired();
            entity.Property(grade => grade.SubjectName).HasMaxLength(200).IsRequired();
            entity.Property(grade => grade.Remarks).HasMaxLength(30).IsRequired();
            entity.Property(grade => grade.Semester).HasMaxLength(50).IsRequired();
            entity.Property(grade => grade.SchoolYear).HasMaxLength(20).IsRequired();
            entity.Property(grade => grade.DatePosted).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_StudentGrades_Grade", "\"Grade\" BETWEEN 0 AND 100"));
            entity.HasOne(grade => grade.Student)
                .WithMany(student => student.Grades)
                .HasForeignKey(grade => grade.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(grade => grade.Faculty)
                .WithMany(faculty => faculty.Grades)
                .HasForeignKey(grade => grade.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(user => user.StudentId).IsUnique();
            entity.HasIndex(user => user.FacultyId).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_AspNetUsers_AcademicRoleMapping",
                "\"StudentId\" IS NULL OR \"FacultyId\" IS NULL"));
            entity.HasOne(user => user.Student)
                .WithMany()
                .HasForeignKey(user => user.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(user => user.Faculty)
                .WithMany()
                .HasForeignKey(user => user.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
