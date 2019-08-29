using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMACD.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SMACD.GUI.Controllers
{
    public class SessionController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SetFile(IFormFile file)
        {
            if (file.Length > 0)
            {
                // full path to file in temp location
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                if (file.FileName.EndsWith(".yaml"))
                {
                    try
                    {
                        Global.ServiceMap = ServiceMapFile.GetServiceMap(filePath);
                        Global.Session = null;
                        Global.LoadedFileName = Path.GetFileName(file.FileName);
                    }
                    catch (Exception ex) { }
                }
                else
                {
                    try
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open))
                        {
                            Global.Session = new SMACD.ScanEngine.Session(fs);
                            Global.ServiceMap = null;
                            Global.LoadedFileName = Path.GetFileName(file.FileName);
                        }
                    }
                    catch (Exception ex) { }
                }
            }

            return new OkResult();
        }

        public IActionResult Artifacts()
        {
            return View();
        }

        public JsonResult ArtifactRoot()
        {
            return new JsonResult(GetArtifactRootDisplay());
        }

        private NodeConfig GetArtifactRootDisplay()
        {
            var root = new NodeConfig();
            root.text = "Root";
            root.state.opened = true;
            
            foreach (var item in Global.Session.Artifacts.ChildNames)
            {
                root.children.Add(IterateArtifactData(Global.Session.Artifacts.GetChildById(item)));
            }
            return root;
        }

        private NodeConfig IterateArtifactData(Artifacts.Artifact artifact)
        {
            var dataNode = new NodeConfig() { text = artifact.Identifier };
            foreach (var childName in artifact.ChildNames)
            {
                dataNode.children.Add(IterateArtifactData(artifact.GetChildById(childName)));
            }
            return dataNode;
        }
    }

    public class NodeConfig
    {
        public string id { get; set; }
        public string text { get; set; }
        public string icon { get; set; }
        public NodeConfigState state { get; set; } = new NodeConfigState();
        public List<NodeConfig> children { get; set; } = new List<NodeConfig>();
        public Dictionary<string, string> li_attr { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> a_attr { get; set; } = new Dictionary<string, string>();
    }
    public class NodeConfigState
    {
        public bool opened { get; set; }
        public bool disabled { get; set; }
        public bool selected { get; set; }
    }
}
