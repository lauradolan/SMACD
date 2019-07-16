using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a plugin and how to run it against an abuse case
    /// </summary>
    public class PluginPointerModel : PointerModel, IModel
    {
        /// <summary>
        ///     Identifier for plugin to use
        /// </summary>
        public string Plugin
        {
            get => TargetIdentifier;
            set => TargetIdentifier = value;
        }

        /// <summary>
        ///     Parameters to pass to plugin to adjust its execution
        /// </summary>
        public Dictionary<string, string> PluginParameters
        {
            get => Parameters;
            set => Parameters = value;
        }

        /// <summary>
        ///     Scorer to use instead of Plugin-provided default
        /// </summary>
        public string Scorer { get; set; }

        /// <summary>
        ///     Resource to pass to plugin
        /// </summary>
        public ResourcePointerModel Resource { get; set; }
    }
}