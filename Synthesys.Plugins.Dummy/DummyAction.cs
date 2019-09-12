using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.Dummy
{
    /// <summary>
    /// This plugin does not do meaningful work and is meant to be an example for future Extension development.
    /// </summary>
    [Extension("dummy",
        Name = "Dummy Action",
        Version = "1.1.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class DummyAction : ActionExtension, IOperateOnHost, ICanQueueTasks
    {
        // The options below are different options that can be used to customize the behavior of an ActionExtension.
        
        /// <summary>
        /// Arbitrary configuration option in string format
        /// </summary>
        [Configurable] public string ConfigurationOption { get; set; }

        /// <summary>
        /// Arbitrary configuration option in integer format, deserialized from a string
        /// </summary>
        [Configurable] public int ConfigurationOption2 { get; set; }

        /// <summary>
        /// Link to the Task toolbox, which can queue Tasks
        /// </summary>
        public ITaskToolbox Tasks { get; set; }

        /// <summary>
        /// Hostname/IP acted upon by the ActionExtension
        ///
        /// This property will only be populated if the ActionExtension is queued with a hostname as a Target. If no compatible Targets were found, this will remain null.
        /// </summary>
        public HostArtifact Host { get; set; }

        /// <summary>
        /// HTTP service acted upon by the ActionExtension
        ///
        /// This property will only be populated if the ActionExtension is queued with an HTTP service as a Target. If no compatible Targets were found, this will remain null.
        ///
        /// This property provides an a more concrete implementation of ServicePortArtifact, which means if a Target is identified as an HTTP server, that Target will be referenced from both this property and the "Service" property below.
        /// </summary>
        public HttpServicePortArtifact HttpService { get; set; }

        /// <summary>
        /// Service acted upon by the ActionExtension, addressed via its port
        ///
        /// This property will only be populated if the ActionExtension is queued with an open port (service) as a Target. If no compatible Targets were found, this will remain null.
        ///
        /// If a more concrete implementation is not matched (for example, because the service was not fingerprinted), the property with the closest parent Type will be referenced.
        /// </summary>
        public ServicePortArtifact Service { get; set; }

        public override ExtensionReport Act()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // Using the "Tasks" property with ICanQueueTasks allows the Extension to address the Task Queue
            Logger.LogInformation($"Task queue running with {Tasks.Count} items");

            var sw = new Stopwatch();
            sw.Start();
            var rng = new Random((int) DateTime.Now.Ticks);
            var v = 0;
            var g = 0;
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
                    var nativePathArtifact = new NativeDirectoryArtifact("dummyBasicContainer");
                    using (var execContainer = nativePathArtifact.GetContext())
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
            // Workspace.Artifacts.Save("dummyData", new { str = "arbitrary!", num = 42 });
            Host.Attachments.Save("dummyResult", new DummyDataClass
            {
                DummyString = "I'm a dummy string!",
                DummyDouble = 42.42
            });

            // Returning an Action-specific Report allows the Action to provide more information
            //   to the user than the Artifact tree
            var random = new Random((int) DateTime.Now.Ticks);
            var randomData = new byte[32];
            random.NextBytes(randomData);

            return new DummySpecificReport
            {
                Data = new DummyDataClass
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