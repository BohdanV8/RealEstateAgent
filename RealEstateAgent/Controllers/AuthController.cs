using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAgent.Data;
using RealEstateAgent.DTOs;
using RealEstateAgent.Models;
using RealEstateAgent.Services;
using System.Security.Claims;

namespace RealEstateAgent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly IJWTService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;
        public AuthController(AppDbContext appDbContext, IJWTService jwtService, UserManager<ApplicationUser> userManager)
        {
            _appDbContext = appDbContext;
            _jwtService = jwtService;
            _userManager = userManager;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterEntity dto)
        {
            if (await _appDbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("User with this email already exists!");
            }
            Guid tenantId = await _appDbContext.Tenants
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == dto.TenantId)
                .Select(s => s.TenantId)
                .FirstOrDefaultAsync();

            if (tenantId == Guid.Empty)
            {
                return BadRequest("Agency not found!");
            }
            ApplicationUser user = new ApplicationUser
            {
                TenantId = tenantId,
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                isActive = false
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            if (result.Succeeded)
            {
                await _userManager.AddToRolesAsync(user, ["RealEstateAgent"]);
            }
            return Ok(
                new {
                    id = user.Id,
                    email = user.Email
                }
            );
        }
        [HttpPost("onboarding")]
        public async Task<IActionResult> Onboarding([FromBody] OnboardingEntity dto)
        {
            if (await _appDbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("User with this email already exists!");
            }
            Tenant tenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = dto.TenantName,
            };
            _appDbContext.Tenants.Add(tenant);
            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                isActive = true,
                UserName = dto.Email
            };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            await _userManager.AddToRolesAsync(user, ["Admin"]);
            string accessToken = _jwtService.GenerateAccessToken(user, ["Admin"]);
            string refreshToken = _jwtService.GenerateRefreshToken();
            RefreshToken refreshTokenEnity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _appDbContext.RefreshTokens.Add(refreshTokenEnity);
            await _appDbContext.SaveChangesAsync();
            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Ok(new
                {
                    id = user.Id,
                    email = user.Email
                }
            );
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginEntity loginEntity)
        {
            var user = await _userManager.FindByEmailAsync(loginEntity.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password!");
            }
            if (await _userManager.CheckPasswordAsync(user, loginEntity.Password))
            {
                if (!user.isActive)
                {
                    return BadRequest("Your account is not active yet. Please wait for the administrator to activate it.");
                }

                IList<string> roles = await _userManager.GetRolesAsync(user);
                string accessToken = _jwtService.GenerateAccessToken(user, roles);
                string refreshToken = _jwtService.GenerateRefreshToken();
                RefreshToken refreshTokenEnity = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                _appDbContext.RefreshTokens.Add(refreshTokenEnity);
                await _appDbContext.SaveChangesAsync();
                Response.Cookies.Append("accessToken", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15)
                });
                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, 
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
                return Ok(new
                {
                    id = user.Id,
                    email = user.Email
                });
            }
            else
            {
                return Unauthorized("Invalid email or password!");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var accessToken = Request.Cookies["accessToken"];
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "Tokens not found in cookies" });
            var principal = _jwtService.ValidateToken(accessToken);
            if (principal == null)
                return Unauthorized(new { message = "Invalid access token" });
            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var storedToken = await _appDbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);
            if (storedToken == null)
                return Unauthorized(new { message = "Invalid refresh token" });

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _appDbContext.RefreshTokens.Remove(storedToken);
                await _appDbContext.SaveChangesAsync();
                return Unauthorized(new { message = "Refresh token expired" });
            }
            IList<string> roles = await _userManager.GetRolesAsync(storedToken.User);
            var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Remove old refresh token
            _appDbContext.RefreshTokens.Remove(storedToken);

            // Add new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _appDbContext.RefreshTokens.Add(newRefreshTokenEntity);
            await _appDbContext.SaveChangesAsync();
            Response.Cookies.Append("accessToken", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Ok(new { message = "Tokens refreshed successfully" });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];  // Read from cookie
            if (!string.IsNullOrEmpty(refreshToken))
            {
                Guid userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                RefreshToken? token = await _appDbContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

                if (token != null)
                {
                    _appDbContext.RefreshTokens.Remove(token);
                    await _appDbContext.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
