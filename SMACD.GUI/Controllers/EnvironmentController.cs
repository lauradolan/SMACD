using Microsoft.AspNetCore.Mvc;
using Synthesys;
using System.Linq;

namespace SMACD.GUI.Controllers
{
    public class EnvironmentController : Controller
    {
        public IActionResult Plugins()
        {
            return View(ExtensionToolbox.Instance.ExtensionLibraries.ToList());
        }
    }
}