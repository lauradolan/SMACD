using Microsoft.Extensions.Logging;
using SMACD.Workspace.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Manages Services that are loaded in the system
    /// </summary>
    public class ServiceToolbox : WorkspaceToolbox
    {
        private Dictionary<string, ServiceInstance> _running = new Dictionary<string, ServiceInstance>();
        public ReadOnlyDictionary<string, ServiceInstance> Running => 
            new ReadOnlyDictionary<string, ServiceInstance>(_running);

        internal ServiceToolbox(Workspace workspace) : base(workspace)
        {
            lock (Running)
            {
                foreach (var descriptor in CurrentWorkspace.Libraries.LoadedServiceDescriptors)
                {
                    BootstrapService(descriptor);
                }
            }
        }

        private void BootstrapService(ServiceDescriptor descriptor)
        {
            Logger.LogInformation("Bootstrapping Workspace service {0}", descriptor.FullServiceId);
            var instance = (ServiceInstance)Activator.CreateInstance(descriptor.ServiceInstanceType, new object[] { });

            var workspaceProperty = instance.GetType().GetProperty("Workspace", BindingFlags.Instance | BindingFlags.NonPublic);
            workspaceProperty.SetValue(instance, CurrentWorkspace);
            _running.Add(descriptor.FullServiceId, instance);

            Logger.LogDebug("Service base configuration complete; running readiness checks");
            if (!instance.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Service {0}; skipping load", descriptor.FullServiceId);
                return;
            }

            Logger.LogTrace("Running service instance Initialization command");
            instance.Initialize();
        }
    }
}
