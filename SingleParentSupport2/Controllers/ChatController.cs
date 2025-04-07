using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            // In a real implementation, you would retrieve the user's chat history
            // and pass it to the view
            return View();
        }

        [HttpPost]
        public IActionResult SendMessage(ChatMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Logic to save chat message would go here
                // In a real implementation, this would likely be an API endpoint
                // that returns JSON for an AJAX request
                return Json(new { success = true });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }
    }
}
