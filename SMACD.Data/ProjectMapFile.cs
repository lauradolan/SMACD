using System;
using System.Collections.Generic;

namespace SMACD.Data
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
    }
}