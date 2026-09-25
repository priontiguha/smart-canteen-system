using canteen_management.DTOs.Auth;
using canteen_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using canteen_management.Data;
using System.Security.Claims;

namespace canteen_management.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, IJwtService jwtService, ApplicationDbContext context)
    {
        _authService = authService;
        _jwtService = jwtService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var user = await _authService.RegisterAsync(request, cancellationToken);
            var response = new AuthResponse
            {
                Message = "Registration successful",
                Token = _jwtService.GenerateToken(user),
                User = new UserSummary
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Registration failed due to a database error." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var user = await _authService.LoginAsync(request, cancellationToken);
            var response = new AuthResponse
            {
                Message = "Login successful",
                Token = _jwtService.GenerateToken(user),
                User = new UserSummary
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Login failed due to a database error." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role.ToString(),
            isActive = user.IsActive
        });
    }
}
