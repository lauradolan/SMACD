using System.Collections.Generic;

namespace SMACD.Shared.Data
{
    /// <summary>
    /// Stores information about a Resource and how to use it with a plugin
    /// </summary>
    public class ResourcePointerModel : IModel
    {
        /// <summary>
        /// Resource identifier to associate with Resource Map
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Parameters to pass to Resource Map
        /// </summary>
        public Dictionary<string, string> ResourceParameters { get; set; }

        /// <summary>
        /// Fingerprint of this data model
        /// </summary>
        public string GetFingerprint() => this.Fingerprint();
    }
}