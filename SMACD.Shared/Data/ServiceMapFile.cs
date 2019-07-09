using System;
using System.Collections.Generic;
using SMACD.Shared.Resources;

namespace SMACD.Shared.Data
{
    /// <summary>
    ///     Service Map - Stores both Features and Resources for an application's test
    /// </summary>
    public class ServiceMapFile : IModel
    {
        /// <summary>
        ///     When this file was created originally
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        ///     When this file was last updated
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        ///     Features stored in this Project Map
        /// </summary>
        public IList<FeatureModel> Features { get; set; } = new List<FeatureModel>();

        /// <summary>
        ///     Resources stored in this Service Map
        /// </summary>
        public IList<Resource> Resources { get; set; } = new List<Resource>();

        /// <summary>
        ///     Fingerprint of this data model
        /// </summary>
        public string GetFingerprint()
        {
            return this.Fingerprint();
        }
    }
}