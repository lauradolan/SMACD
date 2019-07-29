using SMACD.PluginHost.Plugins;
using System;

namespace SMACD.Plugins.Nmap
{
    public class Metadata : ILibraryMetadata
    {
        public string Name => "Nmap Port Scanning Plugin";
        public Version Version => new Version(1, 0, 0);
        public string Author => "Anthony Turner";
        public string Website => "www.anthturner.com";
        public string Description => "Scans a target for open ports.";
    }
}