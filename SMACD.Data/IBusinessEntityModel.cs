using System.Collections.Generic;

namespace SMACD.Data
{
    public interface IBusinessEntityModel
    {
        /// <summary>
        ///     Name of business entity
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Description of business entity
        /// </summary>
        string Description { get; set; }

        /// <summary>
        ///     Business owners for this entity
        /// </summary>
        IList<OwnerPointerModel> Owners { get; set; }

        /// <summary>
        ///     Relative level of risk for this entity in comparison to other entities in this generation
        ///     A high business risk may indicate that the entity manages sensitive data
        /// </summary>
        int BusinessRisk { get; set; }

        /// <summary>
        ///     Relative level of technical risk for this entity in comparison to other entities in this generation
        ///     A high technical risk may indicate that there is a low degree of complexity to technically attack the targets
        ///     associated with this entity
        /// </summary>
        int TechnicalRisk { get; set; }
    }
}