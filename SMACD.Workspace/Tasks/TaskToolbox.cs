using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Libraries;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.Workspace.Tasks
{
    /// <summary>
    /// Manages the Task queue
    /// </summary>
    public class TaskToolbox : WorkspaceToolbox
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

        internal TaskToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Enqueue a Task based on its Descriptor
        /// </summary>
        /// <param name="descriptor">Task Descriptor to enqueue</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        public Task<ActionSpecificReport> Enqueue(TaskDescriptor descriptor)
        {
            ActionInstance actionInstance = CurrentWorkspace.Actions.GetActionInstance(
                descriptor.ActionId,
                descriptor.Options,
                descriptor.TargetIds);

            QueuedTaskDescriptor queuedTaskDescriptor = new QueuedTaskDescriptor()
            {
                ActionId = descriptor.ActionId,
                Options = descriptor.Options,
                TargetIds = descriptor.TargetIds
            };
            var actionInstanceTask = new Task<ActionSpecificReport>(() =>
            {
                TaskStarted?.Invoke(this, queuedTaskDescriptor);
                ActionSpecificReport result = null;
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    try
                    {
                        result = actionInstance.Execute();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error running Action");
                        TaskFaulted?.Invoke(this, queuedTaskDescriptor);
                        result = new ActionSpecificReport();
                    }
                    sw.Stop();

                    queuedTaskDescriptor.Result = result;

                    result.GeneratingTask = queuedTaskDescriptor;
                    result.Runtime = sw.Elapsed;
                    
                    CurrentWorkspace.Reports.Add(result);
                } catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error running Action (from harness)");
                }

                // After completion, trigger any Actions that were tripped by *this* Plugin's execution
                EnqueueTasksTriggeredBy(queuedTaskDescriptor);

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

        internal void EnqueueTasksTriggeredBy(Artifact artifact)
        {
            var descriptors = CurrentWorkspace.Triggers.GetDescriptorsByTriggeringArtifact(artifact);

            foreach (var item in descriptors)
            {
                Enqueue(new TaskDescriptor()
                {
                    ActionId = item.ActionIdentifierCreated,
                    Options = item.DefaultOptionsOnCreation,
                    //TargetIds = descriptor.TargetIds
                    // TODO: No support for Targets here makes this mostly useless :(
                });
            }
        }

        internal void EnqueueTasksTriggeredBy(QueuedTaskDescriptor descriptor)
        {
            // Toolbox finds registered Actions who have "TriggeredBy" attributes matching this
            var actionDescriptors = CurrentWorkspace.Triggers.GetDescriptorsByTriggeringAction(descriptor.ActionId);
            foreach (var item in actionDescriptors)
            {
                Enqueue(new TaskDescriptor()
                {
                    ActionId = item.ActionIdentifierCreated,
                    Options = item.DefaultOptionsOnCreation,
                    TargetIds = descriptor.TargetIds
                });
            }
        }

        internal void EnqueueTasksTriggeredBy(SystemEvents systemEvent)
        {
            var descriptors = CurrentWorkspace.Triggers.GetDescriptorsByTriggeringSystemAction(systemEvent);

            foreach (var item in descriptors)
            {
                Enqueue(new TaskDescriptor()
                {
                    ActionId = item.ActionIdentifierCreated,
                    Options = item.DefaultOptionsOnCreation,
                    //TargetIds = descriptor.TargetIds
                    // TODO: No support for Targets here makes this mostly useless :(
                });
            }
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

                        var totalWidth = (24 + 24 + 10 + 9) + 13; // 13 is the bars and extra spaces
                        var headerWidth = totalWidth - " RUNNING TASKS ".Length;
                        Logger.LogTrace($"{new string('#', headerWidth/2+1)} RUNNING TASKS {new string('#', headerWidth/2)}");
                        Logger.LogTrace($"| {"Action ID".PadRight(24)} | {"Target IDs".PadRight(24)} | {"Status".PadLeft(10)} | {"Resulted?".ToString().PadLeft(9)} |");
                        foreach (var item in RunningTasks)
                            Logger.LogTrace($"| {item.Key.ActionId.PadRight(24)} | {string.Join(", ", item.Key.TargetIds).PadRight(24)} | {item.Key.ActionTask.Status.ToString().PadLeft(10)} | {(item.Key.Result != null).ToString().PadLeft(9)} |");
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
