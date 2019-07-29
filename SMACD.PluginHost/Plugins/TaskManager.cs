using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;

namespace SMACD.PluginHost.Plugins
{
    public class TaskManager
    {
        private const int MAXIMUM_CONCURRENT_ACTIONS = 10;

        public delegate void TaskCompletedDelegate(Task task);

        public delegate void TaskStartedDelegate(Task task);

        private static readonly Lazy<TaskManager> _instance = new Lazy<TaskManager>(() => new TaskManager());
        public static TaskManager Instance { get; } = _instance.Value;

        public bool IsCurrentlyRunning => Count > 0 || (TaskManagerWorkerTask != null && !TaskManagerWorkerTask.IsCompleted);
        public int Count => Stack.Count + CurrentlyRunning.Count;

        public int TotalCompletedTasks { get; private set; }
        private Task TaskManagerWorkerTask { get; set; }
        private ConcurrentDictionary<Task<ScoredResult>, int> CurrentlyRunning { get; } = new ConcurrentDictionary<Task<ScoredResult>, int>();
        private ConcurrentStack<Task<ScoredResult>> Stack { get; } = new ConcurrentStack<Task<ScoredResult>>();
        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskManager");

        /// <summary>
        ///     Fired when a Task is completed
        /// </summary>
        public event TaskCompletedDelegate TaskCompleted;

        /// <summary>
        ///     Fired when a Task is started
        /// </summary>
        public event TaskStartedDelegate TaskStarted;

        public Task<ScoredResult> Enqueue(PluginSummary pluginSummary, bool asDummy = false)
        {
            var plugin = PluginLibrary.PluginsAvailable[pluginSummary.Identifier]
                .CreateInstance(
                    pluginSummary.Options,
                    pluginSummary.ResourceIds.Select(r => ResourceManager.Instance.GetById(r)).ToList());

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
                sw.Start();
                ScoredResult result = null;
                try
                {
                    if (!asDummy)
                        result = plugin.Execute();
                    else
                    {
                        Logger.LogDebug("Treating {0} as DUMMY operation", plugin.PluginDescription.Identifier);
                        var fakeOrigin = new PluginSummary
                        {
                            Identifier = plugin.PluginDescription.Identifier,
                            Options = plugin.Options,
                            ResourceIds = plugin.Resources.Select(r => r.ResourceId).ToList()
                        };
                        result = new ScoredResult { Plugin = fakeOrigin, Scorer = fakeOrigin };
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error running plugin {0} in working directory {1}", plugin.PluginDescription.Identifier, plugin.WorkingDirectory.Location);
                }
                if (result == null)
                {
                    Logger.LogWarning(
                        "Misbehaving plugin {0} returned NULL instead of a blank ScoredResult. Please let the developer know!",
                        plugin.PluginDescription.Identifier);
                    Logger.LogWarning(
                        "Returning impersonated blank ScoredResult -- Plugin and Scorer properties for this may be wrong.");
                    var fakeOrigin = new PluginSummary
                    {
                        Identifier = plugin.PluginDescription.Identifier,
                        Options = plugin.Options,
                        ResourceIds = plugin.Resources.Select(r => r.ResourceId).ToList()
                    };
                    result = new ScoredResult { Plugin = fakeOrigin, Scorer = fakeOrigin };
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
                var lastLog = DateTime.Now;
                while (Count > 0)
                {
                    Thread.Sleep(500);

                    if (DateTime.Now - lastLog > TimeSpan.FromSeconds(15))
                    {
                        Logger.LogDebug("   🏃‍ {0}   |   ⌚ {1}   |   ✔ {2}   ", CurrentlyRunning.Count, Stack.Count, TotalCompletedTasks);
                        lastLog = DateTime.Now;
                    }

                    if (CurrentlyRunning.Count < MAXIMUM_CONCURRENT_ACTIONS && !Stack.IsEmpty)
                    {
                        Stack.TryPop(out var task);
                        CurrentlyRunning.TryAdd(task, 0);
                        TaskStarted?.Invoke(task);
                        task.Start();
                        task.ContinueWith(t =>
                        {
                            TaskCompleted?.Invoke(t);
                            CurrentlyRunning.TryRemove(task, out int dummy);
                            TotalCompletedTasks++;
                        });
                    }
                }
            });
        }
    }
}