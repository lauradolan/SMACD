using System;

namespace SMACD.PluginHost.Plugins
{
    public interface ILibraryMetadata
    {
        string Name { get; }
        Version Version { get; }
        string Author { get; }
        string Website { get; }
        string Description { get; }
    }
}