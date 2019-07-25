using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Plugins.Dummy
{
    // Use the PluginImplementation attribute to specify what type of Plugin this is and what identifier is used
    //   to access it. Identifiers are used inside the framework as "<type>.<name>", such as "attack.dummy". However,
    //   the framework will determine what role is most appropriate for the situation and only request the Plugin by
    //   its name-- in this case, just "dummy".
    [PluginImplementation(PluginTypes.AttackTool, "dummy")]
    public class DummyAttackTool : Plugin
    {
        // Specify the ResourceModel type as needed; when the instance is given to the action, this will
        //   be pre-populated with the specified resource. Ensure inheritance needs match what
        //   is provided (either here or as another parameter)
        // For example, you may want to receive an HttpResource *or* a SocketResource. You
        //   could set the type as "ResourceModel", since they both fit in that Type requirement;
        //   they both inherit from "ResourceModel". You could alternatively write two properties; one for
        //   the HttpResource and another for the SocketResource.
        public HttpResource DummyResource { get; set; }

        // Options that can be configured by the Service Map must be marked with the "Configurable"
        //   attribute. The runtime will take care of casting the string values to their proper
        //   types. If there is a more complicated object, it's suggested to serialize that to a
        //   string and handle that serialization in the Plugin.
        [Configurable] public string ConfigurationOption { get; set; }
        [Configurable] public int ConfigurationOption2 { get; set; }

        public DummyAttackTool(string workingDirectory) : base(workingDirectory)
        {
        }

        public override ScoredResult Execute()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // "WorkingDirectory" is the location where artifacts should be placed by this Plugin
            Logger.LogInformation("Working Directory: {0}", WorkingDirectory);

            // "PluginType" contains the Plugin defining this instance
            Logger.LogInformation("Plugin Type: {0}", PluginDescription.PluginType);

            var sw = new Stopwatch();
            sw.Start();
            var rng = new Random((int)DateTime.Now.Ticks);
            var v = 0;
            var g = 0;
            while (v < 50)
            {
                g++;
                Thread.Sleep(rng.Next(500, 2000));
                v += rng.Next(2, 10);

                Logger.LogInformation("Generation {0} -- {1}/100", g, v);

                // Only test ExecutionWrapper on the first generation
                if (g == 1)
                {
                    // Use "ExecutionWrapper" to run commands whose syntax is identical between OSes
                    // - This provides 2 events, StandardOutputDataReceived and StandardErrorDataReceived, which also link back to the 
                    //   "owner" Task that spawned this external program (to correlate issues in logs)
                    var execFail = new ExecutionWrapper("echo This is a test of a syntax error &&");
                    var execSuccess = new ExecutionWrapper("echo This is a test of a valid output");
                    execFail.StandardErrorDataReceived += (sender, ownerTaskId, data) =>
                    {
                        // When logging information from inside an ExecutionWrapper callback, use Logger.TaskLog*
                        //  functions, which accept a Task ID to correlate these outputs with any outputs from the
                        //  enclosing automation
                        Logger.TaskLogCritical(ownerTaskId, "RECEIVED ERROR: {1}", data);
                    };
                    execSuccess.StandardOutputDataReceived += (sender, ownerTaskId, data) =>
                    {
                        Logger.TaskLogInformation(ownerTaskId, "RECEIVED DATA: {1}", data);
                    };

                    // Keeping all actions within the same Task helps when correlating information later
                    Task.WhenAll(
                        execFail.Start(),
                        execSuccess.Start()).Wait();
                }
            }

            sw.Stop();

            // Using the "SaveResultArtifact" command on the WorkingDirectory is recommended, since it handles serialization
            //   and deserialization in a standard way for all components
            WorkingDirectory.SaveResultArtifact("dummy.dat", new DummyData { Duration = sw.Elapsed, Generations = g });

            Logger.LogInformation("Completed in {0}", sw.Elapsed);

            // Plugins must return a ScoredResult object. *No matter what*, a Runtime will be filled in at the end of the run.

            // This ScoredResult can be created by the attack tool; the Plugin provides a template with Plugin metadata populated.
            var exampleScoredResult = CreateBlankScoredResult();
            exampleScoredResult.RawScore = 256;
            exampleScoredResult.RawScoreMaximum = 300;

            // The attack tool can also return GetScoredResult(), which will allow the runtime to decide which scorer to use.
            // If the Service Map defines an explicit scorer, that will always take precedence.
            // If the Attack Tool specifies a preferred scorer identifier, that will be used.
            // If the Attack Tool does not specify a preferred scorer, a scorer called "score.<attack tool identifier>"
            //   will be used.
            return GetScoredResult();

            // Since there is no preferred scorer specified, the default will be used (see "RegisterActions",
            //   near the top of this file)
        }
    }

    public class DummyData
    {
        public TimeSpan Duration { get; set; }
        public int Generations { get; set; }
    }
}
