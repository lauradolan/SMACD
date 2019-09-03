using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace SMACD.GUI.Controllers
{
    public class EnvironmentController : Controller
    {
        public IActionResult Plugins()
        {
            return View(SMACD.ScanEngine.ExtensionToolbox.Instance.ExtensionLibraries.ToList());
        }
    }
}