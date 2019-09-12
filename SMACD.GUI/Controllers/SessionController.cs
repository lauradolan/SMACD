using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMACD.Artifacts;
using SMACD.Data;
using Synthesys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                        Global.Session = new Session();
                        Global.Session.ServiceMapYaml = System.IO.File.ReadAllText(filePath);
                        foreach (var target in Global.ServiceMap.Targets)
                            Global.Session.RegisterTarget(target);
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
                            Global.Session = new Session(fs);
                            Global.ServiceMap = ServiceMapFile.GetServiceMapFromYaml(Global.Session.ServiceMapYaml);
                            foreach (var target in Global.ServiceMap.Targets)
                                Global.Session.RegisterTarget(target);
                            Global.LoadedFileName = Path.GetFileName(file.FileName);
                        }
                    }
                    catch (Exception ex) { }
                }
            }

            return new OkResult();
        }

        public IActionResult NoFileLoaded()
        {
            return View();
        }

        public IActionResult Artifacts()
        {
            if (!Global.SessionLoaded) return new RedirectToActionResult("NoFileLoaded", "Session", null);
            return View();
        }

        public JsonResult ArtifactRoot()
        {
            return new JsonResult(GetArtifactRootDisplay());
        }

        private NodeConfig GetArtifactRootDisplay()
        {
            var root = new NodeConfig()
            {
                text = "Root",
                icon = "fa fa-tree",
                state = new NodeConfigState { opened = true }
            };
            
            foreach (var child in Global.Session.Artifacts.Children)
            {
                root.children.Add(IterateArtifactData(child));
            }
            return root;
        }

        private NodeConfig IterateArtifactData(Artifacts.Artifact artifact)
        {
            var dataNode = new NodeConfig() { text = artifact.Identifier };

            if (artifact is HostArtifact)
                dataNode.icon = "fa-server";
            if (artifact is ServicePortArtifact)
                dataNode.icon = "fa-plug";
            if (artifact is UrlArtifact)
                dataNode.icon = "fa-map-marker";
            if (artifact is UrlRequestArtifact)
                dataNode.icon = "fa-bolt";

            dataNode.icon = $"fa {dataNode.icon}";

            if (artifact.Children.Any())
            {
                var childrenNode = new NodeConfig { text = "Children" };
                foreach (var child in artifact.Children)
                    childrenNode.children.Add(IterateArtifactData(child));
                dataNode.children.Add(childrenNode);
            }

            if (artifact.Attachments.Any())
            {
                var attachmentNode = new NodeConfig { text = "Attachments", icon = "fa fa-paperclip" };
                foreach (var child in artifact.Attachments)
                    attachmentNode.children.Add(new NodeConfig()
                    {
                        icon = "fa fa-paperclip",
                        text = child.Name
                    });
                dataNode.children.Add(attachmentNode);
            }

            if (artifact.Vulnerabilities.Any())
            {
                var vulnerabilityNode = new NodeConfig { text = "Vulnerabilities", icon = "fa fa-bug" };
                foreach (var vuln in artifact.Vulnerabilities)
                    vulnerabilityNode.children.Add(new NodeConfig
                    {
                        text = vuln.Title,
                        icon = vuln.RiskLevel == Vulnerability.RiskLevels.High ? "fa fa-bolt" : ""
                    });
                dataNode.children.Add(vulnerabilityNode);
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
