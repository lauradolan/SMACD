using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the plugin, such as its name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AttackToolMetadataAttribute : MetadataAttribute
    {
        /// <summary>
        ///     Specify the metadata for the plugin, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to plugin from Service Map</param>
        public AttackToolMetadataAttribute(string identifier) : base(identifier)
        {
        }

        public string DefaultScorer { get; set; }
    }
}