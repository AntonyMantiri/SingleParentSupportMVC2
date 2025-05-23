using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public ChatController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);

            // Get list of volunteers for regular users to chat with
            // Or get list of users for volunteers to chat with
            List<ApplicationUser> chatPartners;

            if (User.IsInRole("Volunteer") || User.IsInRole("Admin"))
            {
                // Volunteers and admins see regular users
                var userList = await _userManager.GetUsersInRoleAsync("User");
                chatPartners = userList.ToList();

            }
            else
            {
                // Regular users see volunteers
                var volunteerList = await _userManager.GetUsersInRoleAsync("Volunteer");
                chatPartners = volunteerList.ToList();


                // Also add admins to the list
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                chatPartners.AddRange(admins);
            }

            // Remove current user from the list if present
            chatPartners = chatPartners.Where(u => u.Id != currentUser.Id).ToList();

            // Pass the data to the view
            ViewBag.CurrentUser = currentUser;
            ViewBag.ChatPartners = chatPartners;

            return View();
        }
    }
}
