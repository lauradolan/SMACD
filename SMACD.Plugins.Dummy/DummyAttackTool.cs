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
    // Use the Implementation attribute to specify what type of Extension this is and what identifier is used
    //   to access it. Identifiers are used inside the framework as "<role>.<name>", such as here, where it would
    //   be "producer.dummy". However, the framework will determine which role is most appropriate for the 
    //   situation and only request the Plugin by its name-- in this case, just "dummy".
    [Implementation(ExtensionRoles.Producer, "dummy")]
    public class DummyAttackTool : ActionInstance
    {
        // Passing a string down to base() allows you to specify the category name of the log events generated
        public DummyAttackTool() : base("DummyAttacker")
        {
        }

        // Specify the TargetDescriptor type as needed; when the instance is created, this will be
        //   pre-populated with the specified target descriptor. Ensure inheritance matches what
        //   is required.
        // For example, you may want to receive an HttpTarget *or* a RawPortTarget. You
        //   could set the type as "TargetDescriptor", since they both fit in that general Type requirement;
        //   they both inherit from "TargetDescriptor".
        // You could instead write two properties; one with the type "HttpTarget" and one with "RawPortTarget".
        public HttpTarget DummyTarget { get; set; }

        // Options that can be configured by the Service Map must be marked with the "Configurable"
        //   attribute. The runtime will take care of casting the string values to their proper
        //   types. If there is a more complicated object, it's suggested to serialize that to a
        //   string and handle the serialization in the Plugin, or flatten the structure.
        [Configurable] public string ConfigurationOption { get; set; }

        [Configurable] public int ConfigurationOption2 { get; set; }

        public override ActionSpecificReport Execute()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // Using the "Workspace" property allows access to other parts of the system and the instance
            Logger.LogInformation("Plugin Type: {0}", Workspace.Actions.GetActionDescriptor(
                RuntimeConfiguration.ActionId).ActionInstanceType);

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
                    // You can use CreateOrLoadNativePath to create an Artifact that allows you to use a disposable Context object
                    //   to get a temporary directory where external tools can persist information to disk or read information.
                    // When the object is disposed, the content of the directory will be compressed and saved to the wrapper
                    //   Artifact. When re-using the context at another time, the directory will be re-deployed.
                    using (var execContainer = Workspace.Artifacts.CreateOrLoadNativePath("dummyBasicContainer", TimeSpan.FromSeconds(60)).GetContext())
                    {
                        // The temporary directory name is stored in the "Directory" property of the context
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
            Logger.LogInformation("Completed in {0}", sw.Elapsed);

            // Artifacts can also be some sort of object (which is serialized by the framework)
            Workspace.Artifacts.Save("dummyData", new { str = "arbitrary!", num = 42 });
            Workspace.Artifacts.Save("dummyResult", new DummyDataClass()
            {
                DummyString = "I'm a dummy string!",
                DummyDouble = 42.42
            });

            // Returning an Action-specific Report allows the Action to provide more information
            //   to the user than the Artifact tree
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

    // When creating Action-specific reports, they must inherit from this framework class
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