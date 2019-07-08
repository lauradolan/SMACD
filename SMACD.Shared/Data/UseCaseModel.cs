using System.Collections.Generic;

namespace SMACD.Shared.Data
{
    /// <summary>
    /// Represents a Use Case: A process executed by a user to accomplish a task as a part of a Feature
    /// </summary>
    public class UseCaseModel : IModel
    {
        /// <summary>
        /// Name of the use case
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the use case does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Business owners of this use case
        /// </summary>
        public IList<OwnerPointerModel> Owners { get; set; } = new List<OwnerPointerModel>();

        /// <summary>
        /// Abuse cases that can be done to misuse this use case
        /// </summary>
        public IList<AbuseCaseModel> AbuseCases { get; set; } = new List<AbuseCaseModel>();

        /// <summary>
        /// Fingerprint of this data model
        /// </summary>
        public string GetFingerprint() => this.Fingerprint();
    }
}