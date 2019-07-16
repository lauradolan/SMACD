using System;
using System.Reflection;

namespace SMACD.ScannerEngine.Attributes
{
    /// <summary>
    /// Specifies the metadata for the Plugin, such as identifier and default Scorer
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginMetadataAttribute : Attribute
    {
        public PluginMetadataAttribute(string identifier)
        {
            Identifier = identifier;
        }

        public string Name { get; set; }
        public string Identifier { get; set; }
        public string DefaultScorer { get; set; }

        public static PluginMetadataAttribute Get<T>()
        {
            return Get(typeof(T));
        }

        public static PluginMetadataAttribute Get(Type t)
        {
            return t.GetCustomAttribute<PluginMetadataAttribute>();
        }
    }
}