using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Feature: A group of Use Cases that, together, accomplish a larger functional task
    /// </summary>
    public class FeatureModel : IBusinessEntityModel, IModel
    {
        /// <summary>
        ///     Use Cases that make up the Feature
        /// </summary>
        public IList<UseCaseModel> UseCases { get; set; } = new List<UseCaseModel>();

        /// <summary>
        ///     Name of the Feature
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Description of what the Feature does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Business owners for the Feature
        /// </summary>
        public IList<OwnerPointerModel> Owners { get; set; } = new List<OwnerPointerModel>();

        /// <summary>
        ///     Relative level of risk for this Feature in comparison to other Features
        ///     A high business risk may indicate that the Feature could access sensitive data
        /// </summary>
        public int BusinessRisk { get; set; }

        /// <summary>
        ///     Relative level of technical risk for this Feature in comparison to other Features
        ///     A high technical risk may indicate that there is a low degree of complexity to technically attack the targets
        ///     associated with this Feature
        /// </summary>
        public int TechnicalRisk { get; set; }
    }
}