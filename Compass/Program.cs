using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SMACD.Data;
using ElectronNET.API;
using SMACD.Artifacts;

namespace Compass
{
    public class Program
    {
        public static string LoadedFileName { get; set; }
        public static ServiceMapFile ServiceMap { get; set; }
        public static Synthesys.Session Session { get; set; }

        public static List<Vulnerability> GetAllVulnerabilities()
        {
            var result = new List<Vulnerability>();
            if (Session != null)
                Get(Session.Artifacts, ref result);
            return result;
        }

        private static void Get(Artifact a, ref List<Vulnerability> list)
        {
            if (list == null) list = new List<Vulnerability>();
            list.AddRange(a.Vulnerabilities);
            foreach (var child in a.Children)
                Get(child, ref list);
        }

        public static void Main(string[] args)
        {
            Synthesys.ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "Plugins"),
                "Synthesys.Plugins.*.dll", true);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseElectron(args);
                });
    }
}
