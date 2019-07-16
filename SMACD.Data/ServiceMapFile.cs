using System;
using System.Collections.Generic;

namespace SMACD.Data
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
    }
}