using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the resource, such as its identifier
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceMetadataAttribute : MetadataAttribute
    {
        /// <summary>
        ///     Specify the metadata for the plugin, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to plugin from Service Map</param>
        public ResourceMetadataAttribute(string identifier) : base(identifier)
        {
        }
    }
}