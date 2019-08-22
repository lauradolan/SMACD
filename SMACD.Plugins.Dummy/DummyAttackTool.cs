using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Capabilities;
using SMACD.SDK.Extensions;
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
    [Extension("dummy",
        Name = "Dummy Action",
        Version = "1.1.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class DummyAttackTool : ActionExtension, IOperateOnHost, IUnderstandProjectInformation, ICanQueueTasks
    {
        // Specify the TargetDescriptor type as needed; when the instance is created, this will be
        //   pre-populated with the specified target descriptor. Ensure inheritance matches what
        //   is required.
        // For example, you may want to receive an HttpTarget *or* a RawPortTarget. You
        //   could set the type as "TargetDescriptor", since they both fit in that general Type requirement;
        //   they both inherit from "TargetDescriptor".
        // You could instead write two properties; one with the type "HttpTarget" and one with "RawPortTarget".
        public HostArtifact Host { get; set; }
        public ProjectPointer ProjectPointer { get; set; }
        public ITaskToolbox Tasks { get; set; }

        // Options that can be configured by the Service Map must be marked with the "Configurable"
        //   attribute. The runtime will take care of casting the string values to their proper
        //   types. If there is a more complicated object, it's suggested to serialize that to a
        //   string and handle the serialization in the Plugin, or flatten the structure.
        [Configurable] public string ConfigurationOption { get; set; }

        [Configurable] public int ConfigurationOption2 { get; set; }


        public override ExtensionReport Act()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // Using the "Tasks" property with ICanQueueTasks allows actions to queue subsequent actions
            Logger.LogInformation($"Task queue running with {Tasks.Count} items");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Random rng = new Random((int)DateTime.Now.Ticks);
            int v = 0;
            int g = 0;
            while (v < 50)
            {
                g++;
                Thread.Sleep(rng.Next(500, 1000));
                v += rng.Next(2, 10);

                Logger.LogInformation("Generation {0} -- {1}/100", g, v);

                // Only test ExecutionWrapper on the first generation
                if (g == 1)
                {
                    // You can use CreateOrLoadNativePath to create an Artifact that allows you to use a disposable Context object
                    //   to get a temporary directory where external tools can persist information to disk or read information.
                    // When the object is disposed, the content of the directory will be compressed and saved to the wrapper
                    //   Artifact. When re-using the context at another time, the directory will be re-deployed.
                    NativeDirectoryArtifact nativePathArtifact = new NativeDirectoryArtifact("dummyBasicContainer");
                    using (NativeDirectoryContext execContainer = nativePathArtifact.GetContext())
                    {
                        // The temporary directory name is stored in the "Directory" property of the context
                        File.WriteAllText(Path.Combine(execContainer.Directory, "test.dat"), "this is a test file!");

                        // Use "ExecutionWrapper" to run commands whose syntax is identical between OSes
                        // - This provides 2 events, StandardOutputDataReceived and StandardErrorDataReceived, which also link back to the
                        //   "owner" Task that spawned this external program (to correlate issues in logs)
                        ExecutionWrapper execFail = new ExecutionWrapper("echo This is a test of a syntax error &&");
                        ExecutionWrapper execSuccess = new ExecutionWrapper("echo This is a test of a valid output");
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
            // Workspace.Artifacts.Save("dummyData", new { str = "arbitrary!", num = 42 });
            Host.Attachments.Save("dummyResult", new DummyDataClass()
            {
                DummyString = "I'm a dummy string!",
                DummyDouble = 42.42
            });

            // Returning an Action-specific Report allows the Action to provide more information
            //   to the user than the Artifact tree
            Random random = new Random((int)DateTime.Now.Ticks);
            byte[] randomData = new byte[32];
            random.NextBytes(randomData);

            return new DummySpecificReport()
            {
                Data = new DummyDataClass()
                {
                    DummyDouble = random.NextDouble(),
                    DummyString = BitConverter.ToString(randomData)
                },
                DummyString = "d4t4!"
            };
        }
    }

    public class DummyDataClass
    {
        public string DummyString { get; set; }
        public double DummyDouble { get; set; }
    }
}