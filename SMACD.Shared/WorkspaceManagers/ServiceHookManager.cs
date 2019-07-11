using System;
using System.Threading.Tasks;
using SMACD.Shared.Attributes;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.WorkspaceManagers
{
    public enum ServiceHookType
    {
        TaskQueueCompleted
    }

    public enum TaskServiceHookType
    {
        TaskStarted,
        TaskCompleted
    }

    /// <summary>
    ///     Handles scanning and mapping of service hooks and integrations
    /// </summary>
    public class ServiceHookManager : LibraryManager<ServiceHook>
    {
        private static readonly Lazy<ServiceHookManager> _instance =
            new Lazy<ServiceHookManager>(() => new ServiceHookManager());

        private ServiceHookManager() : base("SMACD.Services.*.dll", "ServiceHookManager")
        {
        }

        public static ServiceHookManager Instance => _instance.Value;

        public void RegisterHook(ServiceHookType hookType, Action callback)
        {
            switch (hookType)
            {
                case ServiceHookType.TaskQueueCompleted:
                    TaskManager.Instance.TaskQueueDrained += (s, e) => callback();
                    break;
            }
        }

        public void RegisterTaskHook(TaskServiceHookType hookType, Action<Task> callback)
        {
            switch (hookType)
            {
                case TaskServiceHookType.TaskStarted:
                    TaskManager.Instance.TaskStarted += (s, e) => callback(e);
                    break;

                case TaskServiceHookType.TaskCompleted:
                    TaskManager.Instance.TaskCompleted += (s, e) => callback(e);
                    break;
            }
        }

        protected override string GetTypeIdentifier(Type type)
        {
            return type.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Identifier);
        }
    }
}