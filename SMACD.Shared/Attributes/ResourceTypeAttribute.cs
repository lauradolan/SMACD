using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify identifier used in the Service Map to point to this Resource Type
    /// </summary>
    public class ResourceIdentifierAttribute : Attribute
    {
        /// <summary>
        ///     Specify identifier used in the Service Map to point to this Resource Type
        /// </summary>
        /// <param name="type">Identifier used in the Service Map to point to this Resource Type</param>
        public ResourceIdentifierAttribute(string type)
        {
            ResourceIdentifier = type;
        }

        /// <summary>
        ///     Identifier used in the Service Map to point to this Resource Type
        /// </summary>
        public string ResourceIdentifier { get; set; }
    }
}