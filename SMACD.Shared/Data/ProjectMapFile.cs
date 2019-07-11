using System;
using System.Collections.Generic;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Data
{
    /// <summary>
    ///     Project Map - Stores information about Features
    /// </summary>
    public class ProjectMapFile : IModel
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
        ///     Fingerprint of this data model
        /// </summary>
        public string GetFingerprint()
        {
            return this.Fingerprint();
        }
    }
}