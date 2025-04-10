using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;
using System.Threading.Tasks;

namespace SingleParentSupport2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserApiController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/UserApi
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    JoinDate = user.JoinDate,
                    IsVolunteer = user.IsVolunteer,
                    Roles = roles.ToList()
                });
            }

            return Ok(userViewModels);
        }

        // GET: api/UserApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var viewModel = new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                JoinDate = user.JoinDate,
                IsVolunteer = user.IsVolunteer,
                VolunteerRole = user.VolunteerRole,
                VolunteerBio = user.VolunteerBio,
                Roles = roles.ToList()
            };

            return Ok(viewModel);
        }

        // PUT: api/UserApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UserEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.IsVolunteer = model.IsVolunteer;
            user.VolunteerRole = model.VolunteerRole;
            user.VolunteerBio = model.VolunteerBio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(model.SelectedRoles ?? new List<string>()).ToArray();
            var rolesToAdd = model.SelectedRoles?.Except(currentRoles).ToArray() ?? Array.Empty<string>();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            return NoContent();
        }

        // DELETE: api/UserApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return NoContent();
        }

        // GET: api/UserApi/Roles
        [HttpGet("Roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles.Select(r => r.Name));
        }
    }
}
