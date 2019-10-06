using SMACD.Data;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Describes a location in a Service Map which precipitated an event
    /// </summary>
    public class ProjectPointer
    {
        /// <summary>
        ///     Feature which creates the Action
        /// </summary>
        public FeatureModel Feature { get; set; }

        /// <summary>
        ///     Use Case which creates the Action
        /// </summary>
        public UseCaseModel UseCase { get; set; }

        /// <summary>
        ///     Abuse Case which creates the Action
        /// </summary>
        public AbuseCaseModel AbuseCase { get; set; }
    }
}