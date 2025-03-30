using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Submit(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Logic to save contact form submission would go here
                return RedirectToAction("ThankYou");
            }
            return View("Index", model);
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
