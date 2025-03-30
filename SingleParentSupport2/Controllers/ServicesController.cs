using Microsoft.AspNetCore.Mvc;

namespace SingleParentSupport2.Controllers
{
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
