using SMACD.Shared.Data;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SMACD.Shared.Plugins
{
    /// <summary>
    /// Wraps the execution of external (system) tasks run by a plugin
    /// </summary>
    public class ExecutionWrapper
    {
        /// <summary>
        /// Task wrapping this execution
        /// </summary>
        public Task RuntimeTask { get; private set; } = Task.FromResult(default(PluginResult));

        /// <summary>
        /// Command being executed
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Duration of last execution
        /// </summary>
        public TimeSpan ExecutionTime { get; private set; }

        /// <summary>
        /// Process object executing this command
        /// </summary>
        public Process Process { get; private set; } = new Process();

        /// <summary>
        /// Standard output from last execution
        /// </summary>
        public string StdOut { get; set; }

        /// <summary>
        /// Standard error from last execution
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        /// Plugin Pointer originally used in plugin that created this wrapper
        /// </summary>
        public PluginPointerModel PluginPointer { get; set; }

        public ExecutionWrapper()
        {
        }

        public ExecutionWrapper(string cmd) => Command = cmd;

        /// <summary>
        /// Execute the command, wrapped by a Task
        /// </summary>
        /// <returns></returns>
        public Task Start(PluginPointerModel pluginPointer)
        {
            if (string.IsNullOrEmpty(Command))
                throw new Exception("Command has not been set but execution has been requested");

            PluginPointer = pluginPointer;

            RuntimeTask = Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                Process.StartInfo = GetStartInfo(Command);
                Process.Start();

                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                Process.OutputDataReceived += (s, e) => StdOut += e + Environment.NewLine;
                Process.ErrorDataReceived += (s, e) => StdErr += e + Environment.NewLine;

                Process.WaitForExit();

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