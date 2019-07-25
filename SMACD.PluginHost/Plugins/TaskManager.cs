using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.PluginHost.Plugins
{
    public class TaskManager
    {
        private static Lazy<TaskManager> _instance = new Lazy<TaskManager>(() => new TaskManager());
        public static TaskManager Instance { get; } = _instance.Value;

        public delegate void TaskCompletedDelegate(Task task);
        public delegate void TaskStartedDelegate(Task task);

        /// <summary>
        /// Fired when a Task is completed
        /// </summary>
        public event TaskCompletedDelegate TaskCompleted;

        /// <summary>
        /// Fired when a Task is started
        /// </summary>
        public event TaskStartedDelegate TaskStarted;

        private Task TaskManagerWorkerTask { get; set; }
        private ConcurrentStack<Task<ScoredResult>> Stack { get; } = new ConcurrentStack<Task<ScoredResult>>();
        private ILogger Logger { get; set; } = Global.LogFactory.CreateLogger("TaskManager");

        public Task<ScoredResult> Enqueue(PluginSummary pluginSummary)
        {
            var plugin = PluginLibrary.PluginsAvailable[pluginSummary.Identifier]
                .CreateInstance(
                    pluginSummary.Options,
                    pluginSummary.ResourceIds.Select(r => ResourceManager.Instance.GetById(r)).ToList());

            Task<ScoredResult> task = null;
            task = new Task<ScoredResult>(() =>
            {
                plugin.WorkingDirectory.ParentResource.PluginChain.Add(new PluginSummary()
                {
                    Identifier = plugin.PluginDescription.Identifier,
                    Options = plugin.Options,
                    ResourceIds = plugin.Resources.Select(r => r.ResourceId).ToList()
                });

                var sw = new Stopwatch();
                var result = plugin.Execute();
                if (result == null)
                {
                    Logger.LogWarning("Misbehaving plugin {0} returned NULL instead of a blank ScoredResult. Please let the developer know!", plugin.PluginDescription.Identifier);
                    Logger.LogWarning("Returning impersonated blank ScoredResult -- Plugin and Scorer properties for this may be wrong.");
                    var fakeOrigin = new PluginSummary()
                    {
                        Identifier = plugin.PluginDescription.Identifier,
                        Options = plugin.Options,
                        ResourceIds = plugin.Resources.Select(r => r.ResourceId).ToList()
                    };
                    result = new ScoredResult() { Plugin = fakeOrigin, Scorer = fakeOrigin };
                }
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
