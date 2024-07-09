using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace LibraryManagementSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuthDbContext _context;

        public AccountController(
            AuthDbContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles
                                          .Select(r => new { r.Id, r.Name })
                                          .ToListAsync();
            return Ok(roles);
        }

        [HttpGet("getUserInfo")]
        //[Authorize] // Requires authentication to access this endpoint
        public async Task<IActionResult> GetUserInfo(string? UserName = null)
        {
            try
            {
                // Retrieve the current user's ID if UserName is not provided
                if (UserName == null)
                {
                    var currentUserName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(currentUserName))
                    {
                        return NotFound("User not found");
                    }

                    UserName = currentUserName;
                }

                // Execute the SQL query to get the user's information and role name
                var query = @"
                SELECT TOP 1
                    u.Id,
                    u.UserName,
                    r.Name AS RoleName
                FROM 
                    [master].[dbo].[AspNetUsers] u
                INNER JOIN 
                    [master].[dbo].[AspNetUserRoles] ur ON u.Id = ur.UserId
                INNER JOIN 
                    [master].[dbo].[AspNetRoles] r ON ur.RoleId = r.Id
                WHERE 
                    u.UserName = @UserName";

                // Execute the SQL query and map the results to UserInfoDto
                UserInfoDto userInfo = null;

                using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserName", UserName);

                        // Execute the query and read the results
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                userInfo = new UserInfoDto
                                {
                                    UserId = reader["Id"].ToString(),
                                    UserName = UserName, // Set the UserName from input parameter
                                    RoleName = reader["RoleName"].ToString() // Retrieve RoleName from query result
                                };
                            }
                        }
                    }
                }

                if (userInfo == null)
                {
                    return NotFound("User not found");
                }

                // Return the user information
                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = new AppUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, registerDto.Role);

            if (!roleResult.Succeeded)
            {
                return BadRequest(roleResult.Errors);
            }

            return Ok("Registration successful");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var appUser = await _userManager.FindByNameAsync(loginDto.Username);

            if (appUser == null)
                return Unauthorized("Invalid username");

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginDto.Password, false);

            if (!result.Succeeded)
                return Unauthorized("Invalid password");

            return Ok("Login successful");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logout successful");
        }        
    }
}