using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    /// Specify the metadata for the service hook, such as its name
    /// </summary>
    public class ServiceHookMetadataAttribute : Attribute
    {
        /// <summary>
        /// Human-friendly name of plugin
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Single-word identifier that is used to point to plugin from Service Map
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Specify the metadata for the service hook, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to service from Service Map</param>
        public ServiceHookMetadataAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}