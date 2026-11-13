using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.Models;
using StudentGradeManagement.Services;

namespace StudentGradeManagement.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IApiTokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> CreateToken(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email!);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password!,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(DatabaseSeeder.FacultyRole) && !roles.Contains(DatabaseSeeder.StudentRole))
        {
            return Forbid();
        }

        return Ok(await tokenService.CreateAsync(user));
    }
}
