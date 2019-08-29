using Microsoft.AspNetCore.Mvc;

namespace SMACD.GUI.Controllers
{
    public class ServiceMapController : Controller
    {
        public IActionResult ViewMap()
        {
            return View();
        }
    }
}
