using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace SingleParentSupport2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [ExcludeFromCodeCoverage]
    public class SupportRequestApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportRequestApiController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/SupportRequestApi
        [HttpGet]
        public async Task<IActionResult> GetSupportRequests()
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

            return Ok(requests);
        }

        // GET: api/SupportRequestApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupportRequest(int id)
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

            return Ok(supportRequest);
        }

        // POST: api/SupportRequestApi
        [HttpPost]
        public async Task<IActionResult> CreateSupportRequest(SupportRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            return CreatedAtAction(nameof(GetSupportRequest), new { id = supportRequest.Id }, supportRequest);
        }

        // PUT: api/SupportRequestApi/5/respond
        [HttpPut("{id}/respond")]
        [Authorize(Roles = "Admin,Volunteer")]
        public async Task<IActionResult> RespondToSupportRequest(int id, SupportRequestResponseViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var supportRequest = await _context.SupportRequests.FindAsync(id);
            if (supportRequest == null)
            {
                return NotFound();
            }

            supportRequest.Status = model.Status;
            supportRequest.Response = model.Response;
            supportRequest.ResponseDate = DateTime.Now;
            supportRequest.ResponderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupportRequestExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool SupportRequestExists(int id)
        {
            return _context.SupportRequests.Any(e => e.Id == id);
        }
    }
}
