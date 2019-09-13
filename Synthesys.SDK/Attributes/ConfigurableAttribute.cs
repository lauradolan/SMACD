using System;
using System.Runtime.CompilerServices;

namespace Synthesys.SDK.Attributes
{
    /// <summary>
    ///     The ConfigurableAttribute specifies that the decorated property is configurable when the Extension is queued.
    ///     A Dictionary{string,string} is used to configure each Extensions, where the key is the string name of the
    ///     Configurable property, and the value is the string representation of the property's value. When the framework
    ///     invokes this Extension, it will cast the string value to the given Type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurableAttribute : Attribute
    {
        /// <summary>
        ///     Specify that the decorated property is configurable when the Extension is queued
        /// </summary>
        /// <summary>
        ///     Items marked with this Attribute can be configured when the Extension is executed.
        /// </summary>
        /// <remarks>Automatically populated by caller</remarks>
        /// <param name="name">Name of Configurable property (auto-generated from caller)</param>
        public ConfigurableAttribute([CallerMemberName] string name = null)
        {
            Name = name;
        }

        /// <summary>
        ///     Name of the Configurable property
        /// </summary>
        public string Name { get; set; }
    }
}