using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Synthesys.SDK.HostCommands
{
    /// <summary>
    ///     Command executed on the host running the application
    /// </summary>
    public abstract class HostCommand
    {
        /// <summary>
        ///     Invoked when an external process generates data
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="ownerTaskId">Task ID of owner</param>
        /// <param name="data">Data generated</param>
        public delegate void ExternalProcessDataReceived(object sender, int ownerTaskId, string data);

        /// <summary>
        ///     A collection of maps between ManagedThreadId (from ExecutionWrapper) and TaskId (from plugin)
        /// </summary>
        public static ConcurrentDictionary<int, int> Maps = new ConcurrentDictionary<int, int>();

        /// <summary>
        ///     Command executed on the host running the application
        /// </summary>
        protected HostCommand()
        {
            OwnerTaskId = Task.CurrentId.GetValueOrDefault(-1);
            Logger = Global.LogFactory.CreateLogger(GetType().Name);
        }

        /// <summary>
        ///     Logger for command
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Task ID of owner
        /// </summary>
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

        /// <summary>
        ///     Route STDOUT to buffer and event
        /// </summary>
        /// <param name="text">STDOUT text</param>
        protected void HandleStdOut(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (CaptureStdOut)
            {
                StdOut += $"{text}{Environment.NewLine}";
            }

            StandardOutputDataReceived?.Invoke(this, OwnerTaskId, text);
        }

        /// <summary>
        ///     Route STDERR to buffer and event
        /// </summary>
        /// <param name="text">STDERR text</param>
        protected void HandleStdErr(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (CaptureStdErr)
            {
                StdErr += $"{text}{Environment.NewLine}";
            }

            StandardErrorDataReceived?.Invoke(this, OwnerTaskId, text);
        }
    }
}