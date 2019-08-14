using System;
using System.Threading.Tasks;

namespace SMACD.SDK
{
    public interface ITaskToolbox
    {
        bool IsRunning { get; }
        int Count { get; }

        event EventHandler<ResultProvidingTaskDescriptor> TaskCompleted;
        event EventHandler<ResultProvidingTaskDescriptor> TaskFaulted;
        event EventHandler<ResultProvidingTaskDescriptor> TaskStarted;

        Task<ExtensionReport> Enqueue(TaskDescriptor descriptor);
    }
}