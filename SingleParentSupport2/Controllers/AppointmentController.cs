using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var appointments = await _context.Appointments
                .Include(a => a.Volunteer)
                .Where(a => a.UserId == user.Id && a.AppointmentDate >= DateTime.Now && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            if (appointments.Count != 0)
            {
                return View(appointments);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Schedule(AppointmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var appointment = new Appointment
                {
                    UserId = user.Id,
                    VolunteerId = model.VolunteerId,
                    Purpose = model.Purpose,
                    AppointmentDate = model.AppointmentDate,
                    AppointmentTime = model.AppointmentTime,
                    Status = "Scheduled"
                };

                TempData["AppointmentDate"] = model.AppointmentDate.ToString("dd-MM-yyyy");
                TempData["AppointmentTime"] = model.AppointmentTime.ToString();
                TempData["VolunteerName"] = await _userManager.Users
                                                            .Where(u => u.Id == model.VolunteerId)
                                                            .Select(u => u.FirstName + " " + u.LastName)
                                                            .FirstOrDefaultAsync();

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction("Confirmation");
            }

            // reload upcoming appointments if invalid
            var userAppointments = await _context.Appointments
                .Include(a => a.Volunteer)
                .Where(a => a.UserId == _userManager.GetUserId(User) && a.AppointmentDate >= DateTime.Today)
                .ToListAsync();

            return View("Index", userAppointments);
        }

        public IActionResult Confirmation()
        {
            return View();
        }


        public async Task<IActionResult> Reschedule(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Volunteer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var model = new AppointmentViewModel
            {
                AppointmentId = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Purpose = appointment.Purpose,
                VolunteerId = appointment.VolunteerId
            };

            return View("Reschedule", model);
        }

        [HttpPost]
        public async Task<IActionResult> Reschedule(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var appointment = await _context.Appointments.FindAsync(model.AppointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.AppointmentDate = model.AppointmentDate;
            appointment.AppointmentTime = model.AppointmentTime;
            appointment.Purpose = model.Purpose;
            appointment.VolunteerId = model.VolunteerId;
            appointment.Status = "Rescheduled";

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Action to fetch available times for a selected volunteer
        public IActionResult GetAvailableTime(string volunteerId, DateTime date)
        {
            var availableTimes = GetAvailableTimes(volunteerId, date);
            return Json(availableTimes); // Return available times as JSON
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Volunteer)
                .Where(a => a.AppointmentDate >= DateTime.Now && a.Status != "Cancelled")
                .ToListAsync();

            var result = appointments.Select(a => new {
                Date = a.AppointmentDate.ToString("yyyy-MM-dd"),
                Time = a.AppointmentTime,
                User = a.User.FirstName + " " + a.User.LastName,
                Volunteer = a.Volunteer.FirstName + " " + a.Volunteer.LastName,
                a.Purpose
            });

            return Json(result);
        }

        private List<AvailableTime> GetAvailableTimes(string volunteerId, DateTime date)
        {
            List<AvailableTime> availableTimes = new List<AvailableTime>();

            var startOfDay = date.Date.AddHours(9); // 9 AM
            var endOfDay = date.Date.AddHours(21); // 9 PM

            for (var time = startOfDay; time <= endOfDay; time = time.AddHours(1))
            {
                bool isBooked = _context.Appointments.Any(a => a.VolunteerId == volunteerId && a.AppointmentDate.Date == date.Date && a.AppointmentTime == time.ToString("HH:mm") && a.Status != "Cancelled");

                // Format the time for display and use in the slot
                availableTimes.Add(new AvailableTime { Time = time.ToString("HH:mm"), IsAvailable = !isBooked });
            }

            return availableTimes;
        }
    }
}
