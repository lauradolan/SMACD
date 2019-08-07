namespace SMACD.Workspace.Libraries
{
    /// <summary>
    /// Purpose of the extension in the overall system
    /// </summary>
    public enum ExtensionRoles
    {
        Producer,
        Consumer,
        Service,
        Unknown
    }

    /// <summary>
    /// Sources in the system that trigger an Action or Service
    /// </summary>
    public enum TriggerSources
    {
        Action,
        Artifact,
        System
    }

    public enum SystemEvents
    {
        TaskStarted,
        TaskCompleted,
        TaskFaulted,

        TargetAdded,

        ArtifactCreated,
        ArtifactDataSaved
    }
}
