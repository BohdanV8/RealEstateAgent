using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAgent.Models;

namespace RealEstateAgent.Controllers
{
    [ApiController]
    [Route("api/admin/team")]
    [Authorize(Roles = "Admin")]
    public class TeamController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public TeamController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("pending-realtors")]
        public async Task<IActionResult> GetPendingRealtors()
        {
            // Отримуємо всіх неактивованих юзерів
            // Твій Global Query Filter автоматично відфільтрує їх по TenantId поточного адміна!
            List<ApplicationUser> pendingUsers = await _userManager.Users.Where(u => !u.isActive).ToListAsync();

            return Ok(pendingUsers);
        }

        [HttpPut("activate-realtor/{userId}")]
        public async Task<IActionResult> ActivateRealtor(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            user.isActive = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok("Realtor activated successfully.");
        }
    }
}
