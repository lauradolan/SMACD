using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using Synthesys.Tasks.Attributes;

namespace Synthesys.Tasks
{
    /// <summary>
    ///     Manages the Task queue
    /// </summary>
    public class TaskToolbox : ITaskToolbox
    {
        private const int MAXIMUM_CONCURRENT_ACTIONS = 10;

        private ConcurrentQueue<RuntimeTaskDescriptor> QueuedTasks { get; } =
            new ConcurrentQueue<RuntimeTaskDescriptor>();

        private ConcurrentDictionary<RuntimeTaskDescriptor, bool> RunningTasks { get; } =
            new ConcurrentDictionary<RuntimeTaskDescriptor, bool>();

        private Task TaskManagerWorker { get; set; }
        internal int TotalCompletedTasks { get; private set; }

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskToolbox");

        /// <summary>
        ///     Fired when a Task moves from Queued to Running
        /// </summary>
        public event EventHandler<RuntimeTaskDescriptor> TaskStarted;

        /// <summary>
        ///     Fired when a Task moves from Running to Complete
        /// </summary>
        public event EventHandler<RuntimeTaskDescriptor> TaskCompleted;

        /// <summary>
        ///     Fired when a Task moves from Running to Faulted
        /// </summary>
        public event EventHandler<RuntimeTaskDescriptor> TaskFaulted;

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
        public Task<List<ExtensionReport>> Enqueue(string actionIdentifier, Artifact rootArtifact, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr=null)
        {
            if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ActionExtensions.Any(e => e.Key == actionIdentifier)))
                return null;

            var actionExtension = ExtensionToolbox.Instance.EmitAction(actionIdentifier).Configure(rootArtifact, options) as ActionExtension;
            if (actionExtension is ICanQueueTasks)                  ((ICanQueueTasks)actionExtension).Tasks = this;
            if (actionExtension is IUnderstandProjectInformation)   ((IUnderstandProjectInformation)actionExtension).ProjectPointer = serviceMapItemPtr;

            var reactions = new List<ReactionExtension>();
            var runtimeTaskDescriptor = new RuntimeTaskDescriptor() { Artifact = rootArtifact, Extension = actionExtension };
            runtimeTaskDescriptor.Task = new Task<List<ExtensionReport>>(() =>
            {
                TaskStarted?.Invoke(this, runtimeTaskDescriptor);
                var reports = new List<ExtensionReport>();
                bool succeeded = false;
                var sw = new Stopwatch();
                try
                {
                    sw.Start();

                    if (actionExtension == null)
                    {
                        Logger.LogCritical("Requested Action {0} is not loaded in system!", actionIdentifier);
                        reports.Add(ExtensionReport.Error(
                            new Exception($"Requested Action {actionIdentifier} is not loaded in system!")));
                    }
                    else
                    {
                        reports.Add(actionExtension.Act());
                    }

                    sw.Stop();

                    succeeded = true;
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error running Action (from harness)");
                    TaskFaulted?.Invoke(this, runtimeTaskDescriptor);
                    reports.Add(ExtensionReport.Error(ex));
                }

                TaskCompleted?.Invoke(this, runtimeTaskDescriptor);
                RunningTasks.TryRemove(runtimeTaskDescriptor, out var dummy);
                TotalCompletedTasks++;
                
                // If the Action was resolved, run any Triggers
                if (actionExtension != null)
                {
                    var reactions = new List<ReactionExtension>();
                    reactions.AddRange(ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(actionExtension, succeeded ? ExtensionConditionTrigger.Succeeds : ExtensionConditionTrigger.Fails));

                    foreach (var reaction in reactions)
                    {
                        // todo: reaction options?
                        var configuredReaction = reaction.Configure(rootArtifact, new Dictionary<string, string>()) as ReactionExtension;

                        if (configuredReaction is ICanQueueTasks)                   ((ICanQueueTasks)configuredReaction).Tasks = this;
                        if (configuredReaction is IUnderstandProjectInformation)    ((IUnderstandProjectInformation)configuredReaction).ProjectPointer = serviceMapItemPtr;
                    }
                    foreach (var item in reactions)
                    {
                        // todo: Reaction Options are not distinguished from Action options, so we have to ground it here
                        item.Configure(rootArtifact, new Dictionary<string, string>());

                        var report = item.React(TriggerDescriptor.ExtensionTrigger(
                            actionIdentifier,
                            succeeded ? ExtensionConditionTrigger.Succeeds : ExtensionConditionTrigger.Fails));
                        report.ExtensionIdentifier = item.Metadata.ExtensionIdentifier;

                        reports.Add(report);
                    }
                }

                reports.ForEach(r => {
                    r.Runtime = sw.Elapsed;
                    r.AffectedArtifactPaths.Add(string.Join(Artifact.PATH_SEPARATOR, rootArtifact.GetNodesToRoot().Select(a => a.UUID)));
                });

                return reports;
            });

            QueuedTasks.Enqueue(runtimeTaskDescriptor);
            RuntimeManagerStackProcessingLoop();

            return runtimeTaskDescriptor.Task;
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
                        Logger.LogTrace($"{new string('#', headerWidth / 2 + 1)} RUNNING TASKS {new string('#', headerWidth / 2)}");
                        foreach (var item in RunningTasks)
                            Logger.LogTrace($"Action '{item.Key.Extension.GetType().Name}' operating on {item.Key.Artifact}");

                        Logger.LogTrace(new string('#', totalWidth));
                    }

                    if (RunningTasks.Count < MAXIMUM_CONCURRENT_ACTIONS && !QueuedTasks.IsEmpty)
                    {
                        QueuedTasks.TryDequeue(out var task);
                        RunningTasks.TryAdd(task, false);
                        task.Task.Start();
                    }
                }
            });
        }
    }
}