using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the plugin, such as its name
    /// </summary>
    public class PluginMetadataAttribute : Attribute
    {
        /// <summary>
        ///     Specify the metadata for the plugin, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to plugin from Service Map</param>
        public PluginMetadataAttribute(string identifier)
        {
            Identifier = identifier;
        }

        /// <summary>
        ///     Human-friendly name of plugin
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Single-word identifier that is used to point to plugin from Service Map
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        ///     Confidence in this plugin's results/accuracy (for summarizing)
        /// </summary>
        public double Confidence { get; set; } = 1.0;
    }
}