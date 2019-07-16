using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Stores information about a Resource and how to use it with a plugin
    /// </summary>
    public class ResourcePointerModel : PointerModel, IModel
    {
        /// <summary>
        ///     Resource identifier to associate with Resource Map
        /// </summary>
        public string ResourceId
        {
            get => TargetIdentifier;
            set => TargetIdentifier = value;
        }

        /// <summary>
        ///     Parameters to pass to Resource Map
        /// </summary>
        public Dictionary<string, string> ResourceParameters
        {
            get => Parameters;
            set => Parameters = value;
        }
    }
}