using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Represents a Use Case: A process executed by a user to accomplish a task as a part of a Feature
    /// </summary>
    public class UseCaseModel : IBusinessEntityModel, IModel
    {
        /// <summary>
        ///     Abuse cases that can be done to misuse this use case
        /// </summary>
        public IList<AbuseCaseModel> AbuseCases { get; set; } = new List<AbuseCaseModel>();

        /// <summary>
        ///     Name of the use case
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Description of what the use case does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Business owners of this use case
        /// </summary>
        public IList<OwnerPointerModel> Owners { get; set; } = new List<OwnerPointerModel>();

        /// <summary>
        ///     Relative level of risk for this Use Case in comparison to other Use Cases for this Feature
        ///     A high business risk may indicate that the Use Case could access sensitive data
        /// </summary>
        public int? BusinessRisk { get; set; }

        /// <summary>
        ///     Relative level of technical risk for this Use Case in comparison to other Use Cases for this Feature
        ///     A high technical risk may indicate that there is a low degree of complexity to technically attack the targets
        ///     associated with this Use Case
        /// </summary>
        public int? TechnicalRisk { get; set; }
    }
}