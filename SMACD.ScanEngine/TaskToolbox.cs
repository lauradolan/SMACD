using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.SDK;
using SMACD.SDK.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.ScanEngine
{
    /// <summary>
    /// Manages the Task queue
    /// </summary>
    public class TaskToolbox : ITaskToolbox
    {
        private const int MAXIMUM_CONCURRENT_ACTIONS = 10;

        /// <summary>
        /// Fired when a Task moves from Queued to Running
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskStarted;

        /// <summary>
        /// Fired when a Task moves from Running to Complete
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskCompleted;

        /// <summary>
        /// Fired when a Task moves from Running to Faulted
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskFaulted;

        private ConcurrentQueue<QueuedTaskDescriptor> QueuedTasks { get; } = new ConcurrentQueue<QueuedTaskDescriptor>();
        private ConcurrentDictionary<QueuedTaskDescriptor, bool> RunningTasks { get; } = new ConcurrentDictionary<QueuedTaskDescriptor, bool>();
        private Task TaskManagerWorker { get; set; }
        internal int TotalCompletedTasks { get; private set; }

        /// <summary>
        /// If the Task queue is running (has any elements)
        /// </summary>
        public bool IsRunning => RunningTasks.Any() || QueuedTasks.Any();
        public int Count => RunningTasks.Count + QueuedTasks.Count;

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskToolbox");

        private readonly Func<TaskDescriptor, string, Dictionary<string, string>, Artifact, ActionExtension> getAction;
        private readonly Func<TaskDescriptor, ActionExtension, ExtensionConditionTrigger, List<ReactionExtension>> getReactions;

        public TaskToolbox(
            Func<TaskDescriptor, string, Dictionary<string, string>, Artifact, ActionExtension> getAction,
            Func<TaskDescriptor, ActionExtension, ExtensionConditionTrigger, List<ReactionExtension>> getReactions)
        {
            this.getAction = getAction;
            this.getReactions = getReactions;
        }

        /// <summary>
        /// Enqueue a Task based on its Descriptor
        /// </summary>
        /// <param name="descriptor">Task Descriptor to enqueue</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<ExtensionReport> Enqueue(TaskDescriptor descriptor)
        {
            QueuedTaskDescriptor queuedTaskDescriptor = new QueuedTaskDescriptor()
            {
                ActionId = descriptor.ActionId,
                Options = descriptor.Options,
                ArtifactRoot = descriptor.ArtifactRoot
            };

            List<ReactionExtension> reactions = new List<ReactionExtension>();
            Task<ExtensionReport> actionInstanceTask = new Task<ExtensionReport>(() =>
            {
                TaskStarted?.Invoke(this, queuedTaskDescriptor);
                ExtensionReport result = null;
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    ActionExtension actionInstance = null;
                    try
                    {
                        actionInstance = getAction(
                            descriptor,
                            descriptor.ActionId,
                            descriptor.Options,
                            descriptor.ArtifactRoot);

                        result = actionInstance.Act();
                        if (result == null)
                        {
                            result = ExtensionReport.Blank(descriptor.ActionId, descriptor.ArtifactRoot, descriptor.Options);
                        }

                        result.TaskDescriptor = queuedTaskDescriptor;

                        List<ReactionExtension> triggered = getReactions(descriptor, actionInstance, ExtensionConditionTrigger.Succeeds);
                        foreach (ReactionExtension item in triggered)
                        {
                            item.React(TriggerDescriptor.ExtensionTrigger(
                                descriptor.ActionId,
                                ExtensionConditionTrigger.Succeeds));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error running Action");
                        TaskFaulted?.Invoke(this, queuedTaskDescriptor);
                        result = ExtensionReport.Error(
                            queuedTaskDescriptor.ActionId,
                            ex,
                            queuedTaskDescriptor.ArtifactRoot,
                            queuedTaskDescriptor.Options);
                        result.TaskDescriptor = queuedTaskDescriptor;

                        if (actionInstance != null) // only trigger if there isn't a resolution failure
                        {
                            List<ReactionExtension> triggered = getReactions(descriptor, actionInstance, ExtensionConditionTrigger.Fails);
                            foreach (ReactionExtension item in triggered)
                            {
                                item.React(TriggerDescriptor.ExtensionTrigger(
                                    descriptor.ActionId,
                                    ExtensionConditionTrigger.Fails));
                            }
                        }
                    }
                    sw.Stop();

                    if (result == null)
                    {
                        result = ExtensionReport.Blank(
                            queuedTaskDescriptor.ActionId,
                            queuedTaskDescriptor.ArtifactRoot,
                            queuedTaskDescriptor.Options);
                    }

                    queuedTaskDescriptor.Result = result;

                    result.TaskDescriptor = queuedTaskDescriptor;
                    result.Runtime = sw.Elapsed;
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error running Action (from harness)");
                }

                TaskCompleted?.Invoke(this, queuedTaskDescriptor);
                RunningTasks.TryRemove(queuedTaskDescriptor, out bool dummy);
                TotalCompletedTasks++;

                return result;
            });

            queuedTaskDescriptor.ActionTask = actionInstanceTask;
            QueuedTasks.Enqueue(queuedTaskDescriptor);
            RuntimeManagerStackProcessingLoop();

            return actionInstanceTask;
        }

        private void RuntimeManagerStackProcessingLoop()
        {
            if (TaskManagerWorker != null)
            {
                return;
            }

            TaskManagerWorker = Task.Run(() =>
            {
                DateTime lastLog = DateTime.Now;
                while (IsRunning)
                {
                    Thread.Sleep(500);

                    if (DateTime.Now - lastLog > TimeSpan.FromSeconds(10))
                    {
                        Logger.LogDebug("   🏃‍ {0}   |   ⌚ {1}   |   ✔ {2}   ", RunningTasks.Count, QueuedTasks.Count, TotalCompletedTasks);
                        lastLog = DateTime.Now;

                        int totalWidth = (24 + 24 + 10 + 9) + 13; // 13 is the bars and extra spaces
                        int headerWidth = totalWidth - " RUNNING TASKS ".Length;
                        Logger.LogTrace($"{new string('#', headerWidth / 2 + 1)} RUNNING TASKS {new string('#', headerWidth / 2)}");
                        Logger.LogTrace($"| {"Action ID".PadRight(24)} | {"Target IDs".PadRight(24)} | {"Status".PadLeft(10)} | {"Resulted?".ToString().PadLeft(9)} |");
                        foreach (KeyValuePair<QueuedTaskDescriptor, bool> item in RunningTasks)
                        {
                            Logger.LogTrace($"| {item.Key.ActionId.PadRight(24)} | {string.Join(", ", item.Key.ArtifactRoot).PadRight(24)} | {item.Key.ActionTask.Status.ToString().PadLeft(10)} | {(item.Key.Result != null).ToString().PadLeft(9)} |");
                        }

                        Logger.LogTrace(new string('#', totalWidth));
                    }

                    if (RunningTasks.Count < MAXIMUM_CONCURRENT_ACTIONS && !QueuedTasks.IsEmpty)
                    {
                        QueuedTasks.TryDequeue(out QueuedTaskDescriptor task);
                        RunningTasks.TryAdd(task, false);
                        task.ActionTask.Start();
                    }
                }
            });
        }
    }
}
