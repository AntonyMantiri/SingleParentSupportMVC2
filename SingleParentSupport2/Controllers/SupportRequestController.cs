using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    [ExcludeFromCodeCoverage]
    public class SupportRequestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportRequestController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /SupportRequest
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isVolunteer = User.IsInRole("Volunteer");

            IQueryable<SupportRequest> requestsQuery = _context.SupportRequests
                .Include(s => s.User);

            // Filter requests based on user role
            if (!isAdmin && !isVolunteer)
            {
                // Regular users can only see their own requests
                requestsQuery = requestsQuery.Where(s => s.UserId == userId);
            }

            var requests = await requestsQuery.OrderByDescending(s => s.RequestDate).ToListAsync();

            return View(requests);
        }

        // GET: /SupportRequest/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isVolunteer = User.IsInRole("Volunteer");

            var supportRequest = await _context.SupportRequests
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supportRequest == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this request
            if (!isAdmin && !isVolunteer && supportRequest.UserId != userId)
            {
                return Forbid();
            }

            return View(supportRequest);
        }

        // GET: /SupportRequest/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /SupportRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                var supportRequest = new SupportRequest
                {
                    UserId = userId,
                    Name = model.Name ?? user.FirstName + " " + user.LastName,
                    Email = model.Email ?? user.Email,
                    Phone = model.Phone,
                    RequestType = model.RequestType,
                    Description = model.Description,
                    RequestDate = DateTime.Now,
                    Status = "Pending"
                };

                _context.Add(supportRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Confirmation), new { id = supportRequest.Id });
            }
            return View(model);
        }

        // GET: /SupportRequest/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var supportRequest = await _context.SupportRequests.FindAsync(id);
            if (supportRequest == null)
            {
                return NotFound();
            }
            return View(supportRequest);
        }

        // GET: /SupportRequest/Respond/5
        [Authorize(Roles = "Admin,Volunteer")]
        public async Task<IActionResult> Respond(int id)
        {
            var supportRequest = await _context.SupportRequests
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supportRequest == null)
            {
                return NotFound();
            }

            var model = new SupportRequestResponseViewModel
            {
                Id = supportRequest.Id,
                Name = supportRequest.Name,
                Email = supportRequest.Email,
                RequestType = supportRequest.RequestType,
                Description = supportRequest.Description,
                RequestDate = supportRequest.RequestDate,
                Status = supportRequest.Status,
                Response = supportRequest.Response
            };

            return View(model);
        }

        // POST: /SupportRequest/Respond/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Volunteer")]
        public async Task<IActionResult> Respond(SupportRequestResponseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var supportRequest = await _context.SupportRequests.FindAsync(model.Id);
                if (supportRequest == null)
                {
                    return NotFound();
                }

                supportRequest.Status = model.Status;
                supportRequest.Response = model.Response;
                supportRequest.ResponseDate = DateTime.Now;
                supportRequest.ResponderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
