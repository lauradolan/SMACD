using System;
using System.Runtime.CompilerServices;

namespace SMACD.Workspace.Actions.Attributes
{
    /// <summary>
    /// Indicates that the property can be overwritten by explicit Options
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurableAttribute : Attribute
    {
        /// <summary>
        /// Configurable item
        /// </summary>
        /// <param name="name">Name of item to configure (auto-generated from caller)</param>
        public ConfigurableAttribute([CallerMemberName] string name = null)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
