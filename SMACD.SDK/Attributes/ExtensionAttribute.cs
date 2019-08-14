using System;

namespace SMACD.SDK.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExtensionAttribute : Attribute
    {
        public string ExtensionIdentifier { get; set; }

        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public Version VersionObj => new Version(Version);
        public string Website { get; set; }
        public string Description { get; set; }

        public ExtensionAttribute(string extensionId)
        {
            ExtensionIdentifier = extensionId;
        }
    }
}
