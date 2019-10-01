using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;

namespace Synthesys
{
    /// <summary>
    ///     Manages the Task queue
    /// </summary>
    public class TaskToolbox : ITaskToolbox
    {
        private const int MAXIMUM_CONCURRENT_ACTIONS = 10;

        private readonly Func<TaskDescriptor, string, Dictionary<string, string>, Artifact, ActionExtension> getAction;

        private readonly Func<TaskDescriptor, ActionExtension, ExtensionConditionTrigger, List<ReactionExtension>>
            getReactions;

        public TaskToolbox(
            Func<TaskDescriptor, string, Dictionary<string, string>, Artifact, ActionExtension> getAction,
            Func<TaskDescriptor, ActionExtension, ExtensionConditionTrigger, List<ReactionExtension>> getReactions)
        {
            this.getAction = getAction;
            this.getReactions = getReactions;
        }

        private ConcurrentQueue<QueuedTaskDescriptor> QueuedTasks { get; } =
            new ConcurrentQueue<QueuedTaskDescriptor>();

        private ConcurrentDictionary<QueuedTaskDescriptor, bool> RunningTasks { get; } =
            new ConcurrentDictionary<QueuedTaskDescriptor, bool>();

        private Task TaskManagerWorker { get; set; }
        internal int TotalCompletedTasks { get; private set; }

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskToolbox");

        /// <summary>
        ///     Fired when a Task moves from Queued to Running
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskStarted;

        /// <summary>
        ///     Fired when a Task moves from Running to Complete
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskCompleted;

        /// <summary>
        ///     Fired when a Task moves from Running to Faulted
        /// </summary>
        public event EventHandler<ResultProvidingTaskDescriptor> TaskFaulted;

        /// <summary>
        ///     If the Task queue is running (has any elements)
        /// </summary>
        public bool IsRunning => RunningTasks.Any() || QueuedTasks.Any();

        /// <summary>
        ///     Number of tasks running and queued
        /// </summary>
        public int Count => RunningTasks.Count + QueuedTasks.Count;

        /// <summary>
        ///     Enqueue a Task based on its Descriptor
        /// </summary>
        /// <param name="descriptor">Task Descriptor to enqueue</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<List<ExtensionReport>> Enqueue(TaskDescriptor descriptor)
        {
            var queuedTaskDescriptor = new QueuedTaskDescriptor
            {
                ActionId = descriptor.ActionId,
                Options = new Dictionary<string, string>(descriptor.Options),
                ArtifactRoot = descriptor.ArtifactRoot
            };

            var reactions = new List<ReactionExtension>();
            var actionInstanceTask = new Task<List<ExtensionReport>>(() =>
            {
                TaskStarted?.Invoke(this, queuedTaskDescriptor);
                var reports = new List<ExtensionReport>();
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    ActionExtension actionInstance = null;
                    try
                    {
                        actionInstance = getAction(
                            descriptor,
                            descriptor.ActionId,
                            descriptor.Options,
                            descriptor.ArtifactRoot);

                        if (actionInstance == null)
                        {
                            Logger.LogCritical("Requested Action {0} is not loaded in system!", descriptor.ActionId);
                            reports.Add(ExtensionReport.Error(
                                new Exception($"Requested Action {descriptor.ActionId} is not loaded in system!")));
                        }
                        else
                        {
                            reports.Add(actionInstance.Act());
                        }

                        if (!reports.Any()) reports.Add(ExtensionReport.Blank());

                        reports.ForEach(r => r.TaskDescriptor = queuedTaskDescriptor);

                        var triggered = getReactions(descriptor, actionInstance, ExtensionConditionTrigger.Succeeds);
                        foreach (var item in triggered)
                            reports.Add(item.React(TriggerDescriptor.ExtensionTrigger(
                                descriptor.ActionId,
                                ExtensionConditionTrigger.Succeeds)));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error running Action");
                        TaskFaulted?.Invoke(this, queuedTaskDescriptor);
                        reports.Add(ExtensionReport.Error(ex));
                        reports.ForEach(r => r.TaskDescriptor = queuedTaskDescriptor);

                        if (actionInstance != null) // only trigger if there isn't a resolution failure
                        {
                            var triggered = getReactions(descriptor, actionInstance, ExtensionConditionTrigger.Fails);
                            foreach (var item in triggered)
                                item.React(TriggerDescriptor.ExtensionTrigger(
                                    descriptor.ActionId,
                                    ExtensionConditionTrigger.Fails));
                        }
                    }

                    sw.Stop();

                    if (!reports.Any()) reports.Add(ExtensionReport.Blank());

                    queuedTaskDescriptor.Result = reports.First();

                    reports.ForEach(r =>
                    {
                        r.TaskDescriptor = queuedTaskDescriptor;
                        r.Runtime = sw.Elapsed;
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error running Action (from harness)");
                }

                TaskCompleted?.Invoke(this, queuedTaskDescriptor);
                RunningTasks.TryRemove(queuedTaskDescriptor, out var dummy);
                TotalCompletedTasks++;

                return reports;
            });

            queuedTaskDescriptor.ActionTask = actionInstanceTask;
            QueuedTasks.Enqueue(queuedTaskDescriptor);
            RuntimeManagerStackProcessingLoop();

            return actionInstanceTask;
        }

        private void RuntimeManagerStackProcessingLoop()
        {
            if (TaskManagerWorker != null) return;

            TaskManagerWorker = Task.Run(() =>
            {
                var lastLog = DateTime.Now;
                while (IsRunning)
                {
                    Thread.Sleep(500);

                    if (DateTime.Now - lastLog > TimeSpan.FromSeconds(10))
                    {
                        Logger.LogDebug("   🏃‍ {0}   |   ⌚ {1}   |   ✔ {2}   ", RunningTasks.Count, QueuedTasks.Count,
                            TotalCompletedTasks);
                        lastLog = DateTime.Now;

                        var totalWidth = 24 + 24 + 10 + 9 + 13; // 13 is the bars and extra spaces
                        var headerWidth = totalWidth - " RUNNING TASKS ".Length;
                        Logger.LogTrace(
                            $"{new string('#', headerWidth / 2 + 1)} RUNNING TASKS {new string('#', headerWidth / 2)}");
                        Logger.LogTrace(
                            $"| {"Action ID".PadRight(24)} | {"Target IDs".PadRight(24)} | {"Status".PadLeft(10)} | {"Resulted?".PadLeft(9)} |");
                        foreach (var item in RunningTasks)
                            Logger.LogTrace(
                                $"| {item.Key.ActionId.PadRight(24)} | {string.Join(", ", item.Key.ArtifactRoot).PadRight(24)} | {item.Key.ActionTask.Status.ToString().PadLeft(10)} | {(item.Key.Result != null).ToString().PadLeft(9)} |");

                        Logger.LogTrace(new string('#', totalWidth));
                    }

                    if (RunningTasks.Count < MAXIMUM_CONCURRENT_ACTIONS && !QueuedTasks.IsEmpty)
                    {
                        QueuedTasks.TryDequeue(out var task);
                        RunningTasks.TryAdd(task, false);
                        task.ActionTask.Start();
                    }
                }
            });
        }
    }
}