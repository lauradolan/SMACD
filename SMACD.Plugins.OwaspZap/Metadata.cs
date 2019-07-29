using SMACD.PluginHost.Plugins;
using System;

namespace SMACD.Plugins.OwaspZap
{
    public class Metadata : ILibraryMetadata
    {
        public string Name => "OWASP ZAProxy Scanner Plugin";
        public Version Version => new Version(1, 0, 0);
        public string Author => "Anthony Turner";
        public string Website => "www.anthturner.com";
        public string Description => "Scans targets using a ZAP automated scanning configuration.";
    }
}