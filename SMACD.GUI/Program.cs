using ElectronNET.API;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SMACD.GUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Electron.Menu.SetApplicationMenu(new ElectronNET.API.Entities.MenuItem[0]);
            //Electron.Notification.Show(new ElectronNET.API.Entities.NotificationOptions("SMACD GUI", "Welcome to SMACD!"));
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseElectron(args);
    }
}
