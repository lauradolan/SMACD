using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Resources;

namespace SMACD.Plugins.Dummy
{
    // AttackToolMetadata specifies information about the plugin.
    // The "identifier" is the name used on the Service Map to locate this plugin when it needs to be used in some way
    // The "name" is the name of the plugin for log and display purposes
    // The "defaultScorer" is the name of the Scorer plugin used to aggregate results for this plugin by default; this
    //   may be overridden
    [AttackToolMetadata("dummy", Name = "Dummy Attack Tool", DefaultScorer = "dummy")]

    // ValidResources specifies the types of Resource that can be passed to this plugin
    // If something is sent to this plugin that it does not understand, it will cause an error upstream
    [ValidResources(typeof(HttpResource))]
    public class DummyAttackTool : AttackTool
    {
        public override async Task Execute(PluginPointerModel pointer, string workingDirectory)
        {
            // Here, prep and execute anything that generates artifacts to be scored later.
            // This phase will always be the longest and runs in parallel to other tasks (up to a configurable limit, default 10)
            
            var sw = new Stopwatch();
            sw.Start();
            var rng = new Random((int) DateTime.Now.Ticks);
            var v = 0;
            var g = 0;
            while (v < 50)
            {
                g++;
                Thread.Sleep(rng.Next(500, 2000));
                v += rng.Next(2, 10);

                // A "Logger" object is provided by default that is resolved to the identifier of this plugin.
                Logger.LogInformation("Generation {0} -- {1}/100", g, v);

                // Use "ExecutionWrapper" to run commands whose syntax is identical between OSes
                // - This provides 2 events, StandardOutputDataReceived and StandardErrorDataReceived, which also link back to the 
                //   "owner" Task that spawned this external program (to correlate issues in logs)
                var execFail = new ExecutionWrapper($"echo This is a test of a syntax error &&");
                var execSuccess = new ExecutionWrapper($"echo This is a test of a valid output");
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
                    execFail.Start(pointer),
                    execSuccess.Start(pointer)).Wait();
            }

            sw.Stop();

            // Using the "SaveResultArtifact" command on the PluginResultPointer is recommended, since it handles serialization
            //   and deserialization in a standard way for all components
            pointer.SaveResultArtifact("dummy.dat", new DummyData {Duration = sw.Elapsed, Generations = g});

            Logger.LogInformation("Completed in {0}", sw.Elapsed);
        }
    }
}