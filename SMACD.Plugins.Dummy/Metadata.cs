using SMACD.PluginHost.Plugins;
using System;

namespace SMACD.Plugins.Dummy
{
    public class Metadata : ILibraryMetadata
    {
        public string Name => "Dummy Plugin";
        public Version Version => new Version(1, 0, 0);
        public string Author => "Anthony Turner";
        public string Website => "www.anthturner.com";
        public string Description => "Dummy plugin to demo plugin syntax";
    }
}