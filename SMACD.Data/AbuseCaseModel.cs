using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Represents an Abuse Case: a way an attacker can abuse a resource to break a use case
    /// </summary>
    public class AbuseCaseModel : IBusinessEntityModel, IModel
    {
        /// <summary>
        ///     Plugins that can be used to scan for this abuse case
        /// </summary>
        public IList<ActionPointerModel> Actions { get; set; } = new List<ActionPointerModel>();

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
        ///     Relative level of risk for this Abuse Case in comparison to other Abuse Cases for this Use Case
        ///     A high business risk may indicate that the Abuse Case could access sensitive data
        /// </summary>
        public int BusinessRisk { get; set; }

        /// <summary>
        ///     Relative level of technical risk for this Abuse Case in comparison to other Abuse Cases for this Use Case
        ///     A high technical risk may indicate that there is a low degree of complexity to technically attack the targets
        ///     associated with this Abuse Case
        /// </summary>
        public int TechnicalRisk { get; set; }
    }
}