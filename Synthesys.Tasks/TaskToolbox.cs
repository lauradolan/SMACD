using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using Synthesys.Tasks.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SMACD.Data;

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
        ///     Service Map currently in use to generate Tasks
        /// </summary>
        public ServiceMapFile ServiceMap { get; set; }

        /// <summary>
        ///     Enqueue a Task based on its Descriptor
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="rootArtifact">Root app tree node for extension</param>
        /// <param name="options">Action options</param>
        /// <param name="serviceMapItemPtr">Pointer to element in Service Map which queued the Extension</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<List<ExtensionReport>> Enqueue(string extensionIdentifier, AppTreeNode rootArtifact, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null)
        {
            if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ActionExtensions.Any(e => e.Key == extensionIdentifier)))
            {
                return null;
            }

            ActionExtension actionExtension = ExtensionToolbox.Instance.EmitAction(extensionIdentifier).Configure(rootArtifact, options) as ActionExtension;
            ApplyExtensionIntegrations(actionExtension, serviceMapItemPtr);

            if (ServiceMap != null && !IsExtensionAllowed(ServiceMap.EnvironmentSettings, actionExtension.Metadata.ExtensionIdentifier))
            {
                Logger.LogWarning("Attempted to enqueue ActionExtension {0} but was blocked by a white/blacklist rule", actionExtension.Metadata.ExtensionIdentifier);
                return Task.FromResult(new List<ExtensionReport>());
            }

            RuntimeTaskDescriptor runtimeTaskDescriptor = new RuntimeTaskDescriptor() { Artifact = rootArtifact, Extension = actionExtension };
            runtimeTaskDescriptor.Task = new Task<List<ExtensionReport>>(() =>
            {
                TaskStarted?.Invoke(this, runtimeTaskDescriptor);
                List<ExtensionReport> reports = new List<ExtensionReport>();
                bool succeeded = false;
                Stopwatch sw = new Stopwatch();
                try
                {
                    sw.Start();

                    if (actionExtension == null)
                    {
                        Logger.LogCritical("Requested Action {0} is not loaded in system!", extensionIdentifier);
                        reports.Add(ExtensionReport.Error(
                            new Exception($"Requested Action {extensionIdentifier} is not loaded in system!")));
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
                RunningTasks.TryRemove(runtimeTaskDescriptor, out bool dummy);
                TotalCompletedTasks++;

                // If the Action was resolved, run any Triggers
                if (actionExtension != null)
                {
                    List<ReactionExtension> reactions = new List<ReactionExtension>();

                    // create Trigger description from event condition
                    var trigger = TriggerDescriptor.ExtensionTrigger(
                        extensionIdentifier,
                        succeeded ? ExtensionConditionTrigger.Succeeds : ExtensionConditionTrigger.Fails);

                    reactions.AddRange(ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(trigger));

                    foreach (var reaction in reactions)
                    {
                        if (ServiceMap != null && !IsExtensionAllowed(ServiceMap.EnvironmentSettings, reaction.Metadata.ExtensionIdentifier))
                        {
                            Logger.LogInformation("ReactionExtension {0} was attempted to be queued from Trigger {1} but was blocked by a white/blacklist rule", reaction.Metadata.ExtensionIdentifier, trigger);
                            continue;
                        }

                        // todo: reaction options?
                        var configuredReaction = reaction.Configure(rootArtifact, new Dictionary<string, string>()) as ReactionExtension;
                        ApplyExtensionIntegrations(configuredReaction, serviceMapItemPtr);

                        var report = configuredReaction.React(trigger);
                        report.ExtensionIdentifier = configuredReaction.Metadata.ExtensionIdentifier;

                        reports.Add(report);
                    }
                }

                reports.ForEach(r =>
                {
                    r.Runtime = sw.Elapsed;
                    r.AffectedArtifactPaths.Add(rootArtifact.GetUUIDPath());
                });

                return reports;
            });

            QueuedTasks.Enqueue(runtimeTaskDescriptor);
            RuntimeManagerStackProcessingLoop();

            return runtimeTaskDescriptor.Task;
        }

        private void RuntimeManagerStackProcessingLoop()
        {
            if (TaskManagerWorker != null)
            {
                return;
            }

            TaskManagerWorker = Task.Run(() =>
            {
                // Assume console width of 80 chars, assume logger prefix takes up 20 chars = 60 chars
                var totalWidth = 60;
                var headerText = "=[RUNNING TASKS]=";
                

                var lastLog = DateTime.Now;
                while (IsRunning)
                {
                    Thread.Sleep(500);

                    if (DateTime.Now - lastLog > TimeSpan.FromSeconds(10))
                    {
                        Logger.LogDebug("   🏃‍ {0}   |   ⌚ {1}   |   ✔ {2}   ", RunningTasks.Count, QueuedTasks.Count,
                            TotalCompletedTasks);
                        lastLog = DateTime.Now;

                        var lines = new List<string>();
                        RunningTasks.ToList().ForEach(t => lines.Add($"{t.Key.Extension.Metadata.ExtensionIdentifier} => {t.Key.Artifact}"));
                        var longest = lines.Max(l => l.Length) + 4;

                        var header =
                            "┏" +
                            new string('━', (longest - headerText.Length - 2) / 2) +
                            headerText +
                            new string('━', (longest - headerText.Length - 2) / 2) +
                            "┓";
                        var runningTasks = string.Join('\n', lines.Select(l => $"┃ {l.PadRight(longest - 4)} ┃"));
                        var footer = "┗" + new string('━', longest - 2) + "┛";

                        Logger.LogTrace($"\n{header}\n{runningTasks}\n{footer}");
                    }

                    if (RunningTasks.Count >= MAXIMUM_CONCURRENT_ACTIONS || QueuedTasks.IsEmpty) continue;

                    QueuedTasks.TryDequeue(out var task);
                    RunningTasks.TryAdd(task, false);
                    task.Task.Start();
                }
            });
        }

        /// <summary>
        ///     Apply integration properties like "Tasks" or "ProjectPointer" which link to other system elements
        /// </summary>
        /// <param name="extension">Extension to configure</param>
        /// <param name="serviceMapItemPtr">Service Map item to apply, if appropriate</param>
        private void ApplyExtensionIntegrations(Extension extension, ProjectPointer serviceMapItemPtr)
        {
            switch (extension)
            {
                case ICanQueueTasks reactionCast:
                    reactionCast.Tasks = this;
                    break;
                case IUnderstandProjectInformation reactionCast:
                    reactionCast.ProjectPointer = serviceMapItemPtr;
                    break;
                case IUnderstandServiceMaps reactionCast:
                    reactionCast.ServiceMap = ServiceMap;
                    break;
            }
        }

        private static bool IsExtensionAllowed(EnvironmentSettings environmentSettings, string extensionIdentifier)
        {
            var regexTest = "^" + Regex.Escape(extensionIdentifier).Replace("\\?", ".").Replace("\\*", ".*") + "$";

            if (environmentSettings.Whitelist.Any() && !environmentSettings.Whitelist.Any(i => Regex.IsMatch(i, regexTest)))
                return false;

            if (environmentSettings.Blacklist.Any() && environmentSettings.Blacklist.Any(i => Regex.IsMatch(i, regexTest)))
                return false;

            return true;
        }
    }
}