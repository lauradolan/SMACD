namespace SMACD.Workspace.Customizations
{
    public static class Extensions
    {
        /// <summary>
        /// Manage Correlations between interesting Artifacts
        /// 
        /// This is filled and shared among all Actions to be able to make decisions
        ///   among the entire run against the target set
        /// </summary>
        public static ArtifactCorrelation Correlations(this Workspace workspace) => 
            (ArtifactCorrelation)workspace.Artifacts;
    }
}
