﻿using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.Tasks;
using Synthesys.Tasks.Attributes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesys.Plugins.Dummy
{
    /// <summary>
    ///     This plugin does not do meaningful work and is meant to be an example for future Extension development.
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
        ///     Arbitrary configuration option in string format
        /// </summary>
        [Configurable]
        public string ConfigurationOption { get; set; }

        /// <summary>
        ///     Arbitrary configuration option in integer format, deserialized from a string
        /// </summary>
        [Configurable]
        public int ConfigurationOption2 { get; set; }

        /// <summary>
        ///     HTTP service acted upon by the ActionExtension
        ///     This property will only be populated if the ActionExtension is queued with an HTTP service as a Target. If no
        ///     compatible Targets were found, this will remain null.
        ///     This property provides an a more concrete implementation of ServicePortArtifact, which means if a Target is
        ///     identified as an HTTP server, that Target will be referenced from both this property and the "Service" property
        ///     below.
        /// </summary>
        public HttpServiceNode HttpService { get; set; }

        /// <summary>
        ///     Service acted upon by the ActionExtension, addressed via its port
        ///     This property will only be populated if the ActionExtension is queued with an open port (service) as a Target. If
        ///     no compatible Targets were found, this will remain null.
        ///     If a more concrete implementation is not matched (for example, because the service was not fingerprinted), the
        ///     property with the closest parent Type will be referenced.
        /// </summary>
        public ServiceNode Service { get; set; }

        /// <summary>
        ///     Link to the Task toolbox, which can queue Tasks
        /// </summary>
        public ITaskToolbox Tasks { get; set; }

        /// <summary>
        ///     Hostname/IP acted upon by the ActionExtension
        ///     This property will only be populated if the ActionExtension is queued with a hostname as a Target. If no compatible
        ///     Targets were found, this will remain null.
        /// </summary>
        public HostNode Host { get; set; }

        public override ExtensionReport Act()
        {
            // When running the workflow, the plugin provides a few elements pertaining to the current process:

            // "Logger" is a named, generic log facility that is tied into the rest of the running application
            Logger.LogInformation("I'm a log message appearing at the '{0}' level!", "Information");

            // Using the "Tasks" property with ICanQueueTasks allows the Extension to address the Task Queue
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
                    NativeDirectoryEvidence nativePathArtifact = Host.Evidence.CreateOrLoadNativePath("dummyBasicContainer");
                    using (NativeDirectoryContext execContainer = nativePathArtifact.GetContext())
                    {
                        // The temporary directory name is stored in the "Directory" property of the context
                        File.WriteAllText(Path.Combine(execContainer.Directory, "test.dat"), "this is a test file!");

                        // Use "NativeHostCommand" to run commands whose syntax is identical between OSes
                        // - This provides 2 events, StandardOutputDataReceived and StandardErrorDataReceived, which also link back to the
                        //   "owner" Task that spawned this external program (to correlate issues in logs)
                        var execFail = new SDK.HostCommands.NativeHostCommand("echo", "This is a test of a syntax error", "&&");
                        var execSuccess = new SDK.HostCommands.NativeHostCommand("echo", "This is a test of a valid output");
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

                        // You can do the same thing with DockerHostCommand if a container is available; just specify the container image!
                        if (SDK.HostCommands.DockerHostCommand.SupportsDocker())
                        {
                            var dockerCommmand = new SDK.HostCommands.DockerHostCommand("alpine:latest", "echo", "Hello from inside Docker!");
                            dockerCommmand.StandardErrorDataReceived += (sender, ownerTaskId, data) => Logger.TaskLogCritical(ownerTaskId, "RECEIVED DOCKER ERROR: {1}", data.Trim());
                            dockerCommmand.StandardOutputDataReceived += (sender, ownerTaskId, data) => Logger.TaskLogInformation(ownerTaskId, "RECEIVED DOCKER DATA: {1}", data.Trim());
                            Task.WhenAll(dockerCommmand.Start()).Wait();
                        }
                        else
                            Logger.LogInformation("Docker not running (or accessible) on platform, skipping Docker demo.");
                    }
                }
            }

            sw.Stop();
            Logger.LogInformation("Completed in {0}", sw.Elapsed);

            // Artifacts can also be some sort of object (which is serialized by the framework)
            // Workspace.Artifacts.Save("dummyData", new { str = "arbitrary!", num = 42 });
            Host.Evidence.Save("dummyResult", new DummyDataClass
            {
                DummyString = "I'm a dummy string!",
                DummyDouble = 42.42
            });

            // Returning an ExtensionReport with Attachments allows the Action to provide more information
            //   to the user (than the Artifact tree) during later review
            Random random = new Random((int)DateTime.Now.Ticks);
            byte[] randomData = new byte[32];
            random.NextBytes(randomData);

            ExtensionReport report = new ExtensionReport
            {
                ReportSummaryName = typeof(DummyReportSummary).FullName,
                ReportViewName = typeof(DummyReportView).FullName
            };
            report.SetExtensionSpecificReport(new DummyDataClass()
            {
                DummyDouble = random.NextDouble(),
                DummyString = BitConverter.ToString(randomData)
            });

            return report;
        }
    }

    public class DummyDataClass
    {
        public string DummyString { get; set; }
        public double DummyDouble { get; set; }
    }
}