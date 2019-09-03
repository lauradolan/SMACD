using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SMACD.ScanEngine;

namespace SMACD.GUI.Controllers
{
    public class DataController : Controller
    {
        public IActionResult Extensions()
        {
            return new JsonResult(ExtensionToolbox.Instance.GetExtensionMetadata());
        }

        public IActionResult HostTargets()
        {
            return new JsonResult(Global.Session.Artifacts.Children.Select(a => a.Identifier));
        }

        public IActionResult PortTargets()
        {
            return new JsonResult(Global.Session.Artifacts.Children.Select(a => a.Identifier));
        }
    }
}