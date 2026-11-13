using StudentGradeManagement.Models;

namespace StudentGradeManagement.Services;

public interface IApiTokenService
{
    Task<TokenResponse> CreateAsync(ApplicationUser user);
}
