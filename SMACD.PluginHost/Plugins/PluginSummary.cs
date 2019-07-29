using System.Collections.Generic;
using SMACD.PluginHost.Extensions;

namespace SMACD.PluginHost.Plugins
{
    public class PluginSummary
    {
        public string Identifier { get; set; }
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public List<string> ResourceIds { get; set; } = new List<string>();

        public override string ToString()
        {
            return
                $"{PluginTypeExtensions.GetPluginType(Identifier)} {Identifier} running against {{{string.Join(", ", ResourceIds)}}}";
        }
    }
}