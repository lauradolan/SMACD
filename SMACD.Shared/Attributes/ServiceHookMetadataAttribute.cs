using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the service hook, such as its name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceHookMetadataAttribute : MetadataAttribute
    {
        /// <summary>
        ///     Specify the metadata for the service hook, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to service from Service Map</param>
        public ServiceHookMetadataAttribute(string identifier) : base(identifier)
        {
        }
    }
}