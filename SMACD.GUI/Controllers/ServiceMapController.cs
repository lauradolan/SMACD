using Microsoft.AspNetCore.Mvc;

namespace SMACD.GUI.Controllers
{
    public class ServiceMapController : Controller
    {
        public IActionResult NoFileLoaded() => View();
        public IActionResult ViewMap()
        {
            if (!Global.ServiceMapLoaded) return new RedirectToActionResult("NoFileLoaded", "ServiceMap", null);
            return View();
        }
    }
}
