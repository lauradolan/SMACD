using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using System;
using System.IO;

namespace Synthesys.Plugins.Dummy
{
    /// <summary>
    ///     This plugin does not do meaningful work and is meant to be an example for future Extension development.
    /// </summary>
    [Extension("dummyReaction",
        Name = "Dummy Reaction",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [TriggeredBy("dummy", ExtensionConditionTrigger.Succeeds)]
    public class DummyReaction : ReactionExtension, IOperateOnHost, IUnderstandProjectInformation
    {
        /// <summary>
        ///     Hostname/IP which is acted upon by the ReactionExtension. This value is populated by the framework.
        /// </summary>
        public HostNode Host { get; set; }

        /// <summary>
        ///     Information about the business elements used to call this ReactionExtension.
        ///     This is used to identify what business elements are linked to a technical implementation
        /// </summary>
        public ProjectPointer ProjectPointer { get; set; }

        /// <summary>
        ///     This method is called when the Extension is popped from the Task Queue and executed
        /// </summary>
        /// <param name="trigger">Trigger causing the ReactionExtension to fire</param>
        /// <returns></returns>
        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            // A Logger is populated by the framework and attached to the calling executable
            Logger.LogInformation("Executing Dummy Reaction from Trigger {0}", trigger);

            // Data Artifacts can be entire directories, so that external applications can make use of that file data
            string text;
            using (NativeDirectoryContext execContainer = Host.Evidence["dummyBasicContainer"].AsNativeDirectoryEvidence().GetContext())
            {
                text = File.ReadAllText(execContainer.DirectoryWithFile("test.dat"));
                Logger.LogInformation("Text Inside Using: " + text);
            }

            Logger.LogInformation("Text Outside Using: " + text);

            // Artifacts can be retrieved with strong typing
            DummyDataClass stronglyTyped = Host.Evidence["dummyResult"].AsObjectEvidence().Get<DummyDataClass>();

            try
            {
                Logger.LogInformation("+ This runs in a try {}");
            }
            catch (Exception ex)
            {
                // If an error occurs, return ExceptionReport.Error with the generated Exception
                return ExtensionReport.Error(ex);
            }

            // If there is no report to include, just use ExtensionReport.Blank()
            return ExtensionReport.Blank();
        }
    }
}