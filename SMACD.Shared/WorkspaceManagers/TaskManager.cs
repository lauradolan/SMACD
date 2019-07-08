using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Shared.WorkspaceManagers
{
    /// <summary>
    /// Handles Task creation and management for multi-threaded operations
    /// </summary>
    /// <typeparam name="T">Plugin Result type this Task Manager's actions create</typeparam>
    internal class TaskManager
    {
        private const int WORKER_LOOP_LOGGER_INTERVAL_SECS = 15;
        private const int DEFAULT_MAX_CONCURRENT_THREADS = 10;

        private static readonly Lazy<TaskManager> _instance = new Lazy<TaskManager>(() => new TaskManager());
        internal static TaskManager Instance => _instance.Value;

        [ThreadStatic]
        public static Task CurrentTask;

        /// <summary>
        /// Fired when the Task Queue starts its processing loop
        /// The processing loop expires when the queue is drained
        /// </summary>
        public event EventHandler TaskQueueStarted;

        /// <summary>
        /// Fired when the Task Queue is drained
        /// </summary>
        public event EventHandler TaskQueueDrained;

        /// <summary>
        /// Fired when a Task is started
        /// </summary>
        public event EventHandler<Task> TaskStarted;

        /// <summary>
        /// Fired when a Task is completed
        /// </summary>
        public event EventHandler<Task> TaskCompleted;

        public bool IsEmpty => !ActiveTasks.Any() && !ScheduledTasks.Any();

        /// <summary>
        /// Controls if the Task Manager is processing new Tasks or not
        /// </summary>
        public ManualResetEventSlim Running { get; private set; } = new ManualResetEventSlim(true);

        /// <summary>
        /// Maximum concurrent operations run by this manager
        /// </summary>
        public int MaximumConcurrentThreads { get; set; } = DEFAULT_MAX_CONCURRENT_THREADS;

        /// <summary>
        /// Tasks currently being executed (being in this set does not guarantee Task will be started yet) - Dictionary values are unused
        /// </summary>
        private ConcurrentDictionary<Task, bool> ActiveTasks { get; } = new ConcurrentDictionary<Task, bool>();

        /// <summary>
        /// Tasks scheduled for later activation
        /// </summary>
        private ConcurrentQueue<Task> ScheduledTasks { get; } = new ConcurrentQueue<Task>();

        /// <summary>
        /// CLI tool logger
        /// </summary>
        private ILogger Logger { get; } = Extensions.LogFactory.CreateLogger("TaskManager");

        /// <summary>
        /// Worker loop task
        /// </summary>
        private Task TaskManagerWorkerTask { get; set; }

        /// <summary>
        /// Add a Task to the queue
        /// </summary>
        /// <param name="task">Task to add</param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Task<T> task, string taskName = "")
        {
            if (!string.IsNullOrEmpty(taskName))
                task.Tag(taskName);
            ScheduledTasks.Enqueue(task);
            TaskManagerWorkerLoop();
            return task;
        }

        internal TaskManager()
        {
            Logger.LogInformation("TaskManager has been created with {0} maximum concurrent tasks", MaximumConcurrentThreads);
        }

        private void TaskManagerWorkerLoop()
        {
            if (TaskManagerWorkerTask != null && TaskManagerWorkerTask.Status == TaskStatus.Running)
                return;

            TaskQueueStarted?.Invoke(this, new EventArgs());

            var lastLog = DateTime.Now;
            TaskManagerWorkerTask = Task.Run(() =>
            {
                Logger.LogInformation("Started QueueManagerTask loop at Task ID {0}", Task.CurrentId);
                while (ActiveTasks.Any() || ScheduledTasks.Any())
                {
                    if (!Running.Wait(500))
                        continue;
                    else
                        Thread.Sleep(500);

                    if ((DateTime.Now - lastLog).Seconds > WORKER_LOOP_LOGGER_INTERVAL_SECS)
                    {
                        lastLog = DateTime.Now;
                        Logger.LogInformation("Active: {1}   <{#!#}>   Running: {2}   <{#!#}>   Scheduled: {3}",
                            ActiveTasks.Count,
                            ActiveTasks.Count(t => t.Key.Status == TaskStatus.Running),
                            ScheduledTasks.Count);
                    }

                    lock (ActiveTasks)
                    {
                        foreach (var task in ActiveTasks.Where(t => t.Key.Status == TaskStatus.Created))
                        {
                            Logger.LogDebug("Starting Task ID {0}", task.Key.Id);
                            try
                            {
                                task.Key.Start();
                                task.Key.ContinueWith(t =>
                                {
                                    ActiveTasks.TryRemove(t, out bool emptyVar);
                                    Logger.LogDebug("Task ID {0} has completed", t.Id);
                                    TaskCompleted?.Invoke(this, task.Key);
                                });
                                TaskStarted?.Invoke(this, task.Key);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogCritical(ex, "Error starting task {0}", task.Key.Id);
                            }
                        }
                    }

                    while (ActiveTasks.Count < MaximumConcurrentThreads && ScheduledTasks.Any())
                    {
                        // Keep TaskManager full to maximum by dequeueing scheduled tasks
                        // TODO: If these Try*s fail, weird things will happen; need to handle false cases
                        if (ScheduledTasks.TryDequeue(out Task task))
                        {
                            Logger.LogDebug("Moving status of Task {0} from {1} to {2} ", task.Id, "scheduled", "active");
                            ActiveTasks.TryAdd(task, false);
                        }
                    }
                }
                TaskManagerWorkerTask = null;
                TaskQueueDrained?.Invoke(this, new EventArgs());
                Logger.LogDebug("Task Queue drained");
            });
            //TaskManagerWorkerTask.Tag(WorkspaceId, true);
        }
    }
}