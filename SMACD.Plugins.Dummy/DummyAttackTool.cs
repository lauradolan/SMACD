using Microsoft.Extensions.Logging;
using SMACD.Workspace;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Plugins.Dummy
{
    // Use the PluginImplementation attribute to specify what type of Plugin this is and what identifier is used
    //   to access it. Identifiers are used inside the framework as "<type>.<name>", such as "attack.dummy". However,
    //   the framework will determine what role is most appropriate for the situation and only request the Plugin by
    //   its name-- in this case, just "dummy".
    [Implementation(ExtensionRoles.Producer, "dummy")]
    public class DummyAttackTool : ActionInstance
    {
        public DummyAttackTool() : base("DummyAttacker")
        {
        }

        // Specify the ResourceModel type as needed; when the instance is given to the action, this will
        //   be pre-populated with the specified resource. Ensure inheritance needs match what
        //   is provided (either here or as another parameter)
        // For example, you may want to receive an HttpResource *or* a SocketResource. You
        //   could set the type as "ResourceModel", since they both fit in that Type requirement;
        //   they both inherit from "ResourceModel". You could alternatively write two properties; one for
        //   the HttpResource and another for the SocketResource.
        public HttpTarget DummyTarget { get; set; }

        // Options that can be configured by the Service Map must be marked with the "Configurable"
        //   attribute. The runtime will take care of casting the string values to their proper
        //   types. If there is a more complicated object, it's suggested to serialize that to a
        //   string and handle that serialization in the Plugin.
        [Configurable] public string ConfigurationOption { get; set; }

        [Configurable] public int ConfigurationOption2 { get; set; }

        public override ActionSpecificReport Execute()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // Using the "Workspace" property allows access to other parts of the system and the instance
            Logger.LogInformation("Plugin Type: {0}", Workspace.Actions.GetActionDescriptor(RuntimeConfiguration.ActionId).ActionInstanceType);

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
                    // Using waitallocate since it's a short op
                    using (var execContainer = Workspace.Artifacts.CreateOrLoadNativePath("dummyBasicContainer", TimeSpan.FromSeconds(60)).GetContext())
                    {
                        File.WriteAllText(Path.Combine(execContainer.Directory, "test.dat"), "this is a test file!");

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
            }

            sw.Stop();

            Workspace.Artifacts.Save("dummyData", new { str = "arbitrary!", num = 42 });
            Workspace.Artifacts.Save("dummyResult", new DummyDataClass()
            {
                DummyString = "I'm a dummy string!",
                DummyDouble = 42.42
            });

            Logger.LogInformation("Completed in {0}", sw.Elapsed);

            return new DummySpecificReport()
            {
                Data = new DummyDataClass()
                {
                    DummyDouble = 80.08,
                    DummyString = "8008"
                },
                DummyString = "d4t4!"
            };
        }
    }

    public class DummySpecificReport : ActionSpecificReport
    {
        public string DummyString { get; set; }
        public DummyDataClass Data { get; set; }
    }

    public class DummyDataClass
    {
        public string DummyString { get; set; }
        public double DummyDouble { get; set; }
    }
}