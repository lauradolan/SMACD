using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Libraries.Attributes;
using System.IO;

namespace SMACD.Plugins.Dummy
{
    // This Action is Triggered by the execution of "producer.dummy"
    [Implementation(ExtensionRoles.Consumer, "dummy")]
    [TriggeredBy(TriggerSources.Action, "producer.dummy")]
    public class DummyScorer : ActionInstance
    {
        public DummyScorer() : base("DummyScorer") { }

        public override ActionSpecificReport Execute()
        {
            Logger.LogInformation("Running Dummy Scorer");

            // Other Actions can load named Artifacts and read or write to them
            string text;
            using (var execContainer = Workspace.Artifacts["dummyBasicContainer"].AsNativeDirectoryArtifact().GetContext())
            {
                text = File.ReadAllText(Path.Combine(execContainer.Directory, "test.dat"));
                Logger.LogInformation("Text: " + text);
            }

            // Artifacts can be retrieved with or without strong typing
            var serializedObject = Workspace.Artifacts["dummyData"].As<ObjectArtifact>().Get();
            var stronglyTyped = Workspace.Artifacts["dummyResult"].As<ObjectArtifact>().Get<DummyDataClass>();

            // If there is no report to include, just use Blank()
            return ActionSpecificReport.Blank();
        }
    }
}