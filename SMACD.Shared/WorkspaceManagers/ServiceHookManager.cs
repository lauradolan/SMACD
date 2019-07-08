using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;

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
    /// Handles scanning and mapping of service hooks and integrations
    /// </summary>
    internal class ServiceHookManager : LibraryManager<Plugin>
    {
        private static readonly Lazy<ServiceHookManager> _instance = new Lazy<ServiceHookManager>(() => new ServiceHookManager());
        internal static ServiceHookManager Instance => _instance.Value;

        private ServiceHookManager() : base("SMACD.Services.*.dll", "ServiceHookManager")
        {
        }

        public void RegisterHook(ServiceHookType hookType, Action callback)
        {
            switch (hookType)
            {
                case ServiceHookType.TaskQueueCompleted:
                    TaskManager.Instance.TaskQueueDrained += (s, e) => callback();
                    break;
            }
        }

        public void RegisterHook(TaskServiceHookType hookType, Action<Task> callback)
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

        protected override string GetTypeIdentifier(Type type) => type.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Identifier);
    }
}