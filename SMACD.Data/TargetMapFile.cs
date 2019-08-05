using System;
using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Target Map - Stores Target endpoints for an application
    /// </summary>
    public class TargetMapFile : IModel
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
        ///     Targets stored in this Target Map
        /// </summary>
        public IList<TargetModel> Targets { get; set; } = new List<TargetModel>();
    }
}