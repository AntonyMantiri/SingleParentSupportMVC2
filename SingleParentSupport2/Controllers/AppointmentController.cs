using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        public IActionResult Index()
        {
            // In a real implementation, you would retrieve the user's appointments
            // and pass them to the view
            return View();
        }

        [HttpPost]
        public IActionResult Schedule(AppointmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Logic to save appointment would go here
                return RedirectToAction("Confirmation");
            }
            return View("Index", model);
        }

        public IActionResult Confirmation()
        {
            return View();
        }

        public IActionResult Reschedule(int id)
        {
            // Logic to get appointment by id would go here
            return View();
        }

        public IActionResult Cancel(int id)
        {
            // Logic to cancel appointment would go here
            return RedirectToAction("Index");
        }
    }
}
