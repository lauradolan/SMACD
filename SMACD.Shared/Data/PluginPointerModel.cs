using System.Collections.Generic;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Data
{
    /// <summary>
    ///     Stores information about a plugin and how to run it against an abuse case
    /// </summary>
    public class PluginPointerModel : IModel
    {
        /// <summary>
        ///     Identifier for plugin to use
        /// </summary>
        public string Plugin { get; set; }

        /// <summary>
        ///     Parameters to pass to plugin to adjust its execution
        /// </summary>
        public IDictionary<string, string> PluginParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Resource to pass to plugin
        /// </summary>
        public ResourcePointerModel Resource { get; set; }

        /// <summary>
        ///     Fingerprint of this data model
        /// </summary>
        public string GetFingerprint()
        {
            return this.Fingerprint();
        }
    }
}