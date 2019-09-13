using System;

namespace Synthesys.SDK.Attributes
{
    /// <summary>
    ///     The ExtensionAttribute specifies the unique name used to address the Extension within the system and metadata about
    ///     the Extension and author
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExtensionAttribute : Attribute
    {
        /// <summary>
        ///     Specify that this class implements an Extension
        /// </summary>
        /// <param name="extensionId">System-unique string to identify this Extension in logs and when being manually queued</param>
        public ExtensionAttribute(string extensionId)
        {
            ExtensionIdentifier = extensionId;
        }

        /// <summary>
        ///     System-unique string to identify this Extension in logs and when being manually queued. Once this is set, changing
        ///     it will break data objects dependent on this Extension.
        /// </summary>
        public string ExtensionIdentifier { get; set; }

        /// <summary>
        ///     Display-friendly name of the Extension
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Person or people who developed the Extension
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        ///     Version of the Extension
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     Version of the Extension, as a Version object
        /// </summary>
        public Version VersionObj => new Version(Version);

        /// <summary>
        ///     Location where more information and updates about this Extension can be found
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        ///     How and what the Extension accomplishes in more detail
        /// </summary>
        public string Description { get; set; }
    }
}