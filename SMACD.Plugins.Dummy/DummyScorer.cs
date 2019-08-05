using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Libraries.Attributes;
using System.IO;

namespace SMACD.Plugins.Dummy
{
    [Implementation(ExtensionRoles.Consumer, "dummy")]
    [TriggeredBy(TriggerSources.Action, "producer.dummy")] // triggered by producer.dummy executing
    public class DummyScorer : ActionInstance
    {
        public DummyScorer() : base("DummyScorer") { }

        public override ActionSpecificReport Execute()
        {
            Logger.LogInformation("Running Dummy Scorer");

            // LoadResultArtifact can be called from the Scorer instance, to load the information stored to the working
            //   directory by SaveResultArtifact. Again, this handles resolving the working directory.
            string text;
            using (var execContainer = Workspace.Artifacts["dummyBasicContainer"].AsNativeDirectoryArtifact().GetContext())
            {
                text = File.ReadAllText(Path.Combine(execContainer.Directory, "test.dat"));
                Logger.LogInformation("Text: " + text);
            }

            var serializedObject = Workspace.Artifacts["dummyData"].As<ObjectArtifact>().Get();
            //var serializedObject = ((ObjectArtifact)Workspace.Artifacts["dummyData"]).Get();
            //var stronglyTyped = ((ObjectArtifact)Workspace.Artifacts["dummyResult"]).Get<DummyDataClass>();
            var stronglyTyped = Workspace.Artifacts["dummyResult"].As<ObjectArtifact>().Get<DummyDataClass>();

            return ActionSpecificReport.Blank;
        }
    }
}