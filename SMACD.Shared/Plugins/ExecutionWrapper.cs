using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Data;

namespace SMACD.Shared.Plugins
{
    /// <summary>
    ///     Wraps the execution of external (system) tasks run by a plugin
    /// </summary>
    public class ExecutionWrapper
    {
        public ExecutionWrapper()
        {
        }

        public ExecutionWrapper(string cmd)
        {
            Command = cmd;
        }

        public static IDictionary<int, int> Maps = new ConcurrentDictionary<int, int>();

        /// <summary>
        ///     Task wrapping this execution
        /// </summary>
        //public Task RuntimeTask { get; private set; } = Task.FromResult(default(Scorer));
        public Task RuntimeTask { get; private set; } = Task.FromResult(0);

        /// <summary>
        ///     Command being executed
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        ///     Duration of last execution
        /// </summary>
        public TimeSpan ExecutionTime { get; private set; }

        /// <summary>
        ///     Process object executing this command
        /// </summary>
        private Process Process { get; } = new Process();

        /// <summary>
        ///     Standard output from last execution
        /// </summary>
        public string StdOut { get; set; }

        /// <summary>
        ///     Standard error from last execution
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        ///     Plugin Pointer originally used in plugin that created this wrapper
        /// </summary>
        public PluginPointerModel PluginPointer { get; set; }

        public delegate void ExternalProcessDataReceived(object sender, int ownerTaskId, string data);
        public event ExternalProcessDataReceived StandardOutputDataReceived;
        public event ExternalProcessDataReceived StandardErrorDataReceived;

        private ILogger Logger { get; } = Workspace.LogFactory.CreateLogger("ExecutionWrapper");
        private int OwnerTaskId { get; set; }

        /// <summary>
        ///     Execute the command, wrapped by a Task
        /// </summary>
        /// <returns></returns>
        public Task Start(PluginPointerModel pluginPointer)
        {
            if (string.IsNullOrEmpty(Command))
                throw new Exception("Command has not been set but execution has been requested");

            PluginPointer = pluginPointer;
            OwnerTaskId = Task.CurrentId.GetValueOrDefault(-1);
            RuntimeTask = Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                Process.StartInfo = GetStartInfo(Command);
                Process.Start();

                Logger.LogTrace("Started process {0}", Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                Process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    StdOut += e.Data + Environment.NewLine;
                    StandardOutputDataReceived?.Invoke(s, OwnerTaskId, e.Data);
                };
                Process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    StdErr += e.Data + Environment.NewLine;
                    StandardErrorDataReceived?.Invoke(s, OwnerTaskId, e.Data);
                };

                Process.WaitForExit();
                Logger.LogTrace("Process {0} completed", Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

                sw.Stop();
                ExecutionTime = sw.Elapsed;
            });

            return RuntimeTask;
        }

        private ProcessStartInfo GetStartInfo(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            ProcessStartInfo procStartInfo;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                procStartInfo = new ProcessStartInfo("cmd", "/c " + escapedArgs);
            else
                procStartInfo = new ProcessStartInfo("/bin/bash", $"-c \"{escapedArgs}\"");

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            return procStartInfo;
        }
    }
}