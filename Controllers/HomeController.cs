using Microsoft.AspNetCore.Mvc;

namespace MedServices.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Start()
        {
            return View();
        }
    }
}
