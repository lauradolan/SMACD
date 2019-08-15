using SMACD.Data;

namespace SMACD.Artifacts
{
    public class ProjectPointer
    {
        /// <summary>
        /// Feature which creates the Action
        /// </summary>
        public FeatureModel Feature { get; set; }

        /// <summary>
        /// Use Case which creates the Action
        /// </summary>
        public UseCaseModel UseCase { get; set; }

        /// <summary>
        /// Abuse Case which creates the Action
        /// </summary>
        public AbuseCaseModel AbuseCase { get; set; }
    }
}
