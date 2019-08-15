using System;

namespace SMACD.SDK.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExtensionAttribute : Attribute
    {
        /// <summary>
        /// Identifier used to call extension or identify it
        /// </summary>
        public string ExtensionIdentifier { get; set; }

        /// <summary>
        /// Extension display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Extension author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Extension version string
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Extension version string
        /// </summary>
        public Version VersionObj => new Version(Version);

        /// <summary>
        /// Website for more information
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// Description of extension
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Specify that this class implements an Extension
        /// </summary>
        /// <param name="extensionId">Extension identifier</param>
        public ExtensionAttribute(string extensionId)
        {
            ExtensionIdentifier = extensionId;
        }
    }
}
