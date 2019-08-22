using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Capabilities;
using SMACD.SDK.Extensions;
using SMACD.SDK.Triggers;
using System.IO;

namespace SMACD.Plugins.Dummy
{
    // This Action is Triggered by the execution of "dummy"
    [Extension("dummyReaction",
        Name = "Dummy Reaction",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [TriggeredBy("dummy", ExtensionConditionTrigger.Succeeds)]
    public class DummyScorer : ReactionExtension, IOperateOnHost
    {
        public HostArtifact Host { get; set; }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            Logger.LogInformation("Running Dummy Scorer from Trigger " + trigger);

            // Other Actions can load named Artifacts and read or write to them
            string text;
            using (NativeDirectoryContext execContainer = Host.Attachments["dummyBasicContainer"].AsNativeDirectoryArtifact().GetContext())
            {
                text = File.ReadAllText(Path.Combine(execContainer.Directory, "test.dat"));
                Logger.LogInformation("Text: " + text);
            }

            // Artifacts can be retrieved with strong typing
            DummyDataClass stronglyTyped = Host.Attachments["dummyResult"].As<ObjectArtifact>().Get<DummyDataClass>();

            // If there is no report to include, just use Blank() or return null
            return ExtensionReport.Blank();
        }
    }
}