using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;

namespace SMACD.Plugins.Dummy
{
    public class TaskManager
    {
        public delegate void TaskCompletedDelegate(Task task);

        public delegate void TaskStartedDelegate(Task task);

        private static readonly Lazy<TaskManager> _instance = new Lazy<TaskManager>(() => new TaskManager());
        public static TaskManager Instance { get; } = _instance.Value;

        private Task TaskManagerWorkerTask { get; set; }
        private ConcurrentStack<Task<ScoredResult>> Stack { get; } = new ConcurrentStack<Task<ScoredResult>>();

        /// <summary>
        ///     Fired when a Task is completed
        /// </summary>
        public event TaskCompletedDelegate TaskCompleted;

        /// <summary>
        ///     Fired when a Task is started
        /// </summary>
        public event TaskStartedDelegate TaskStarted;

        public Task<ScoredResult> Enqueue(Plugin plugin)
        {
            Task<ScoredResult> task = null;
            task = new Task<ScoredResult>(() =>
            {
                plugin.WorkingDirectory.ParentResource.Configuration.Plugins.Add(new PluginSummary
                {
                    Identifier = plugin.PluginDescription.Identifier,
                    Options = plugin.Options,
                    ResourceIds = plugin.Resources.Select(r => r.ResourceId).ToList()
                });
                plugin.WorkingDirectory.ParentResource.Commit();

                var sw = new Stopwatch();
                var result = plugin.Execute();
                sw.Stop();
                result.Runtime = sw.Elapsed;
                return result;
            });

            Stack.Push(task);
            RuntimeManagerStackProcessingLoop();

            return task;
        }

        private void RuntimeManagerStackProcessingLoop()
        {
            if (TaskManagerWorkerTask != null)
                return;

            TaskManagerWorkerTask = Task.Run(() =>
            {
                while (!Stack.IsEmpty)
                {
                    Thread.Sleep(500);
                    Task<ScoredResult> task;
                    Stack.TryPop(out task);

                    TaskStarted?.Invoke(task);
                    task.Start();
                    TaskCompleted?.Invoke(task);
                }
            });
        }
    }
}