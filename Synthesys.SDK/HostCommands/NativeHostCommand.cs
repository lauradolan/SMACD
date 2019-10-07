using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Synthesys.SDK.HostCommands
{
    public class NativeHostCommand : HostCommand, IDisposable
    {
        public NativeHostCommand(string command, params string[] args)
        {
            ProcessStartInfo = new ProcessStartInfo
            {
                FileName = Path.GetFileName(command),
                WorkingDirectory = Path.GetDirectoryName(command),
                Arguments = string.Join(' ', args).Replace("\"", "\\\""),

                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        protected ProcessStartInfo ProcessStartInfo { get; set; }
        protected Process Process { get; set; } = new Process();

        public bool ValidateCommandExists()
        {
            if (ProcessStartInfo == null)
            {
                return false;
            }

            if (!File.Exists(Path.Combine(
                ProcessStartInfo.WorkingDirectory,
                ProcessStartInfo.FileName)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Execute the command, wrapped by a Task
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            if (ProcessStartInfo == null)
            {
                throw new Exception("ProcessStartInfo was not set but execution has been requested");
            }

            RuntimeTask = Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                if (!Process.Start())
                {
                    Logger.TaskLogCritical(OwnerTaskId, "Process {0} failed to execute!",
                        Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);
                    FailedToExecute = true;
                    return;
                }

                Logger.TaskLogTrace(OwnerTaskId, "Started process {0}",
                    Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                Process.OutputDataReceived += (s, e) => HandleStdOut(e.Data);
                Process.ErrorDataReceived += (s, e) => HandleStdErr(e.Data);

                Process.WaitForExit();
                Logger.TaskLogTrace(OwnerTaskId, "Process {0} completed",
                    Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);

                sw.Stop();
                ExecutionTime = sw.Elapsed;
            });

            return RuntimeTask;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!Process.HasExited)
                    {
                        Process.Kill();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}