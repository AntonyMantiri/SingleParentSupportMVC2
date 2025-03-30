using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    public class SupportRequestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(SupportRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Logic to save support request would go here
                return RedirectToAction("Confirmation");
            }
            return View(model);
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
