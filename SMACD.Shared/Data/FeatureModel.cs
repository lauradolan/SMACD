using System.Collections.Generic;

namespace SMACD.Shared.Data
{
    /// <summary>
    /// Represents a Feature: A group of Use Cases that, together, accomplish a larger functional task
    /// </summary>
    public class FeatureModel : IModel
    {
        /// <summary>
        /// Name of the feature
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the feature does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Business owners for the feature
        /// </summary>
        public IList<OwnerPointerModel> Owners { get; set; } = new List<OwnerPointerModel>();

        /// <summary>
        /// Use Cases that make up the feature
        /// </summary>
        public IList<UseCaseModel> UseCases { get; set; } = new List<UseCaseModel>();

        /// <summary>
        /// Fingerprint of this data model
        /// </summary>
        public string GetFingerprint() => this.Fingerprint();
    }
}