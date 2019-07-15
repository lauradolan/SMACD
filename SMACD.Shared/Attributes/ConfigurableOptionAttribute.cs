using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specifies an option that can be configured by the user
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ConfigurableOptionAttribute : Attribute
    {
        /// <summary>
        ///     Name of configuration option
        /// </summary>
        public string OptionName { get; set; }

        /// <summary>
        ///     Default value for configuration option
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        ///     If this option is required for the plugin to execute
        /// </summary>
        public bool Required { get; set; }
    }
}