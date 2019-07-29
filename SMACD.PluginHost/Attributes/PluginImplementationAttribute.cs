using SMACD.PluginHost.Extensions;
using System;

namespace SMACD.PluginHost.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginImplementationAttribute : Attribute
    {
        /// <summary>
        ///     Denotes this class as implementing a Plugin when a specific identifier is used
        /// </summary>
        /// <param name="type">Plugin type</param>
        /// <param name="identifier">Plugin identifier</param>
        public PluginImplementationAttribute(PluginTypes type, string identifier)
        {
            Type = type;
            Identifier = identifier;
        }

        /// <summary>
        ///     Plugin Type -- this will end up prefixing the Identifier
        /// </summary>
        public PluginTypes Type { get; }

        /// <summary>
        ///     Identifier (without type) for this Plugin
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        ///     Get the Identifier with PluginType prefixed
        /// </summary>
        public string FullIdentifier => $"{Type.GetPluginTypeString()}.{Identifier}";
    }
}