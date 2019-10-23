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
using System.Reflection;
using Synthesys.SDK.Attributes;

namespace Synthesys.Tasks
{
    /// <summary>
    ///     Manages the Task queue
    /// </summary>
    public class TaskToolbox : ITaskToolbox
    {
        private const int MAXIMUM_CONCURRENT_ACTIONS = 5;

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
        ///     Enqueue a ReactionExtension based on its Descriptor
        /// </summary>
        /// <param name="trigger">Trigger creating the Reaction</param>
        /// <param name="extensionIdentifier">ReactionExtension identifier</param>
        /// <param name="rootNode">Root of AppTree</param>
        /// <param name="options">Reaction options</param>
        /// <param name="serviceMapItemPtr">Pointer to element in Service Map which queued the Extension</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<ExtensionReport> Enqueue(TriggerDescriptor trigger, string extensionIdentifier, RootNode rootNode, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null)
        {
            if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ReactionExtensions.Any(e => e.Value.Any(t => t.GetCustomAttribute<ExtensionAttribute>()?.ExtensionIdentifier == extensionIdentifier))))
            {
                return null;
            }

            var task = GetTaskDescriptor(
                ExtensionToolbox.Instance.EmitReaction(extensionIdentifier), 
                rootNode, 
                options, 
                trigger,
                serviceMapItemPtr);
            QueuedTasks.Enqueue(task);
            RuntimeManagerStackProcessingLoop();
            return task.Task;
        }

        /// <summary>
        ///     Enqueue an ActionExtension based on its Descriptor
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="appTreeNode">Root app tree node for extension</param>
        /// <param name="options">Action options</param>
        /// <param name="serviceMapItemPtr">Pointer to element in Service Map which queued the Extension</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<ExtensionReport> Enqueue(string extensionIdentifier, AppTreeNode appTreeNode, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null)
        {
            if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ActionExtensions.Any(e => e.Key == extensionIdentifier)))
            {
                return null;
            }

            var task = GetTaskDescriptor(
                ExtensionToolbox.Instance.EmitAction(extensionIdentifier), 
                appTreeNode, 
                options,
                null,
                serviceMapItemPtr);

            QueuedTasks.Enqueue(task);
            RuntimeManagerStackProcessingLoop();
            return task.Task;
        }

        /// <summary>
        ///     Create a RuntimeTaskDescriptor with a Task configured to execute a given Extension by its identifier
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier to resolve</param>
        /// <param name="appTreeNode">AppTreeNode to pass into Extension</param>
        /// <param name="options">Options to pass</param>
        /// <param name="serviceMapItemPtr">Pointer to item in Service Map causing this execution</param>
        /// <returns></returns>
        private RuntimeTaskDescriptor GetTaskDescriptor(Extension extension, AppTreeNode appTreeNode, Dictionary<string, string> options, TriggerDescriptor trigger = null, ProjectPointer serviceMapItemPtr = null)
        { 
            // Configures all [Configurable], applies AppTreeNodes to their strong types, and adds RootNodes where needed
            extension = extension.Configure(appTreeNode, options);

            // Adds system integrations, such as Service Map awareness and Task queueing
            ApplyExtensionIntegrations(extension, serviceMapItemPtr);

            RuntimeTaskDescriptor runtimeTaskDescriptor = new RuntimeTaskDescriptor() { Artifact = appTreeNode, Extension = extension, Trigger = trigger };
            if (ServiceMap != null && !IsExtensionAllowed(ServiceMap.EnvironmentSettings, extension.Metadata.ExtensionIdentifier))
            {
                Logger.LogWarning("Attempted to enqueue Extension {0} but was blocked by a whitelist/blacklist rule", extension.Metadata.ExtensionIdentifier);
                runtimeTaskDescriptor.Task = Task.FromResult(ExtensionReport.Error(new InvalidOperationException("Blocked by whitelist/blacklist rule")));
            }
            else
            {
                runtimeTaskDescriptor.Task = new Task<ExtensionReport>(() =>
                {
                    var report = ExecuteExtension(runtimeTaskDescriptor);

                    // create Trigger description from event condition
                    var trigger = TriggerDescriptor.ExtensionTrigger(
                            extension.Metadata.ExtensionIdentifier,
                            report.ErrorEncountered == null ? ExtensionConditionTrigger.Succeeds : ExtensionConditionTrigger.Fails);

                    QueueReactions(trigger, appTreeNode is RootNode ? appTreeNode as RootNode : appTreeNode.Root, serviceMapItemPtr);
                    return report;
                }, TaskCreationOptions.LongRunning);
            }
            return runtimeTaskDescriptor;
        }

        /// <summary>
        ///     Execute the Task body of a queued Extension
        /// </summary>
        /// <param name="runtimeTaskDescriptor">Task descriptor</param>
        /// <returns></returns>
        private ExtensionReport ExecuteExtension(RuntimeTaskDescriptor runtimeTaskDescriptor)
        {
            var extension = runtimeTaskDescriptor.Extension;
            TaskStarted?.Invoke(this, runtimeTaskDescriptor);
            ExtensionReport report = ExtensionReport.Blank();
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();

                if (extension is ActionExtension)
                    report = ((ActionExtension)extension).Act();
                else
                    report = ((ReactionExtension)extension).React(runtimeTaskDescriptor.Trigger);

                sw.Stop();

                report.ExtensionIdentifier = extension.Metadata.ExtensionIdentifier;
                report.AffectedArtifactPaths = new List<string>() { runtimeTaskDescriptor.Artifact.GetUUIDPath() };
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error running Action (from harness)");
                TaskFaulted?.Invoke(this, runtimeTaskDescriptor);
                report = ExtensionReport.Error(ex);
            }

            TaskCompleted?.Invoke(this, runtimeTaskDescriptor);
            RunningTasks.TryRemove(runtimeTaskDescriptor, out var _);
            TotalCompletedTasks++;

            return report;
        }

        /// <summary>
        ///     Queue Reactions which occurred as the result of a given Trigger
        /// </summary>
        /// <param name="trigger">Trigger which executed the Reaction</param>
        /// <param name="rootNode">Root node of AppTree</param>
        /// <param name="projectPointer">Pointer to Service Map location originating the Action causing this</param>
        private void QueueReactions(TriggerDescriptor trigger, RootNode rootNode, ProjectPointer projectPointer)
        {
            var reactions = ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(trigger);
            Logger.LogTrace("Processing {0} ReactionExtensions triggered by {1}", reactions.Count, trigger);

            foreach (var reaction in reactions)
            {
                var metadata = reaction.GetCustomAttribute<ExtensionAttribute>();
                Logger.LogTrace("Attempting to queue Reaction {0}", metadata.ExtensionIdentifier);
                Enqueue(trigger, metadata.ExtensionIdentifier, rootNode, new Dictionary<string, string>(), projectPointer);
            }
        }

        /// <summary>
        ///     Run stack processing loop for Tasks
        /// </summary>
        private void RuntimeManagerStackProcessingLoop()
        {
            if (TaskManagerWorker != null)
            {
                return;
            }

            TaskManagerWorker = Task.Run(() =>
            {
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
                    task.Task.Start(TaskScheduler.Default);
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

        /// <summary>
        ///     Check if Extension can be executed based on permissiveness
        /// </summary>
        /// <param name="environmentSettings">Environment settings</param>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <returns></returns>
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