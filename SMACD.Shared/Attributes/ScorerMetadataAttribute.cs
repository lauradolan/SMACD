using System;

namespace SMACD.Shared.Attributes
{
    /// <summary>
    ///     Specify the metadata for the scorer, such as its name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScorerMetadataAttribute : MetadataAttribute
    {
        /// <summary>
        ///     Specify the metadata for the scorer, such as its name
        /// </summary>
        /// <param name="identifier">Single-word identifier that is used to point to scorer from Service Map</param>
        public ScorerMetadataAttribute(string identifier) : base(identifier)
        {
        }

        public string DefaultScorer { get; }
    }
}