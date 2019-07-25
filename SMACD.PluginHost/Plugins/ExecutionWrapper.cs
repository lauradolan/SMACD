using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Extensions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SMACD.PluginHost.Plugins
{
    /// <summary>
    ///     Wraps the execution of external (system) tasks run by a plugin
    /// </summary>
    public class ExecutionWrapper
    {
        public delegate void ExternalProcessDataReceived(object sender, int ownerTaskId, string data);

        /// <summary>
        /// A collection of maps between ManagedThreadId (from ExecutionWrapper) and TaskId (from plugin)
        /// </summary>
        public static ConcurrentDictionary<int, int> Maps = new ConcurrentDictionary<int, int>();

        public ExecutionWrapper()
        {
            OwnerTaskId = Task.CurrentId.GetValueOrDefault(-1);
        }

        public ExecutionWrapper(string cmd) : this()
        {
            Command = cmd;
        }

        /// <summary>
        ///     Task wrapping this execution
        /// </summary>
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

        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("ExecutionWrapper");
        private int OwnerTaskId { get; }
        public event ExternalProcessDataReceived StandardOutputDataReceived;
        public event ExternalProcessDataReceived StandardErrorDataReceived;

        /// <summary>
        ///     Execute the command, wrapped by a Task
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            if (string.IsNullOrEmpty(Command))
                throw new Exception("Command was not set but execution has been requested");

            RuntimeTask = Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                Process.StartInfo = GetStartInfo(Command);
                Process.Start();

                Logger.TaskLogTrace(OwnerTaskId, "Started process {0}",
                    Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

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
                Logger.TaskLogTrace(OwnerTaskId, "Process {0} completed",
                    Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

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