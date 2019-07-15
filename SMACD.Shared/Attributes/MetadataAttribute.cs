using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the plugin, such as its name
    /// </summary>
    public abstract class MetadataAttribute : Attribute
    {
        /// <summary>
        ///     Specify the metadata for the plugin, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to plugin from Service Map</param>
        protected MetadataAttribute(string identifier)
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
    }
}