using System.Collections.Generic;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Data
{
    /// <summary>
    ///     Represents an Abuse Case: a way an attacker can abuse a resource to break a use case
    /// </summary>
    public class AbuseCaseModel : IModel
    {
        /// <summary>
        ///     Name of abuse case
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Description of how abuse case works
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Business owners of this abuse case
        /// </summary>
        public IList<OwnerPointerModel> Owners { get; set; } = new List<OwnerPointerModel>();

        /// <summary>
        ///     Plugins that can be used to scan for this abuse case
        /// </summary>
        public IList<PluginPointerModel> PluginPointers { get; set; } = new List<PluginPointerModel>();

        /// <summary>
        ///     Fingerprint of this data model
        /// </summary>
        public string GetFingerprint()
        {
            return this.Fingerprint();
        }
    }
}