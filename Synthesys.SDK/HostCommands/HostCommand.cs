using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Synthesys.SDK.HostCommands
{
    public abstract class HostCommand
    {
        public delegate void ExternalProcessDataReceived(object sender, int ownerTaskId, string data);

        /// <summary>
        ///     A collection of maps between ManagedThreadId (from ExecutionWrapper) and TaskId (from plugin)
        /// </summary>
        public static ConcurrentDictionary<int, int> Maps = new ConcurrentDictionary<int, int>();

        protected HostCommand()
        {
            OwnerTaskId = Task.CurrentId.GetValueOrDefault(-1);
            Logger = Global.LogFactory.CreateLogger(GetType().Name);
        }

        protected ILogger Logger { get; }
        protected int OwnerTaskId { get; }

        /// <summary>
        ///     Task wrapping this execution
        /// </summary>
        public Task RuntimeTask { get; protected set; } = Task.FromResult(0);

        /// <summary>
        ///     Duration of last execution
        /// </summary>
        public TimeSpan ExecutionTime { get; protected set; }

        /// <summary>
        ///     Process object executing this command
        /// </summary>
        private Process Process { get; } = new Process();

        /// <summary>
        ///     If STDOUT should be captured and stored in StdOut property
        /// </summary>
        public bool CaptureStdOut { get; set; }

        /// <summary>
        ///     If STDERR should be captured and stored in StdErr property
        /// </summary>
        public bool CaptureStdErr { get; set; }

        /// <summary>
        ///     Standard output from last execution
        /// </summary>
        public string StdOut { get; protected set; }

        /// <summary>
        ///     Standard error from last execution
        /// </summary>
        public string StdErr { get; protected set; }

        /// <summary>
        ///     If this process failed to execute
        /// </summary>
        public bool FailedToExecute { get; protected set; }

        /// <summary>
        ///     Fired when data is written to STDOUT
        /// </summary>
        public event ExternalProcessDataReceived StandardOutputDataReceived;

        /// <summary>
        ///     Fired when data is written to STDERR
        /// </summary>
        public event ExternalProcessDataReceived StandardErrorDataReceived;

        protected void HandleStdOut(string text)
        {
            if (CaptureStdOut) StdOut += $"{text}{Environment.NewLine}";
            StandardOutputDataReceived?.Invoke(this, OwnerTaskId, text);
        }

        protected void HandleStdErr(string text)
        {
            if (CaptureStdErr) StdErr += $"{text}{Environment.NewLine}";
            StandardErrorDataReceived?.Invoke(this, OwnerTaskId, text);
        }
    }
}