using ElectronNET.API;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;

namespace SMACD.GUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Electron.Menu.SetApplicationMenu(new ElectronNET.API.Entities.MenuItem[0]);
            //Electron.Notification.Show(new ElectronNET.API.Entities.NotificationOptions("SMACD GUI", "Welcome to SMACD!"));

            SMACD.ScanEngine.ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"),
                    "SMACD.Plugins.*.dll");

            AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;

            CreateWebHostBuilder(args).Build().Run();
        }

        private static System.Reflection.Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        {
            return SMACD.ScanEngine.ExtensionToolbox.Instance.ExtensionLibraries.Select(
                l => l.Assembly.GetType(args.Name)).FirstOrDefault(t => t != null)?.Assembly;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseElectron(args);
    }
}
