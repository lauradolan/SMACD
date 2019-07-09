using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;

namespace SMACD.Plugins.Dummy
{
    [PluginMetadata("dummy", Name = "Dummy Plugin")]
    public class DummyPlugin : Plugin
    {
        public override Task<PluginResult> Execute(PluginPointerModel pointer, string workingDirectory)
        {
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
                Logger.LogInformation("Generation {0} -- {1}/100", g, v);

                var exec = new ExecutionWrapper("echo This is a test of the echo back");
                exec.Process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) Logger.LogCritical("RECEIVED DATA: {1}", e.Data);
                };
                var task = exec.Start(pointer);
                task.Wait();
            }

            sw.Stop();

            Logger.LogInformation("Completed in {0}", sw.Elapsed);

            return Task.FromResult((PluginResult) new DummyPluginResult(pointer, workingDirectory)
            {
                Generations = g,
                Duration = sw.Elapsed
            });
        }

        public override Task<PluginResult> Reprocess(string workingDirectory)
        {
            return Task.FromResult((PluginResult) new DummyPluginResult(workingDirectory));
        }
    }
}