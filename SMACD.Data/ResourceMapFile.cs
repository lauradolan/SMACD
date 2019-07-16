using System;
using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Resource Map - Stores endpoints for an application
    /// </summary>
    public class ResourceMapFile : IModel
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
        ///     Resources stored in this Resource Map
        /// </summary>
        public IList<Resource> Resources { get; set; } = new List<Resource>();
    }
}