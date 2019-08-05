using SMACD.Workspace.Services;
using System;
using System.Linq;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Manages Services that are loaded in the system
    /// </summary>
    public class ServiceToolbox : WorkspaceToolbox
    {
        internal ServiceToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Retrieve the ServiceDescriptor for a given Service
        /// </summary>
        /// <param name="serviceIdentifier">Service Identifier</param>
        /// <returns></returns>
        public ServiceDescriptor GetServiceDescriptor(string serviceIdentifier) =>
            CurrentWorkspace.Libraries.LoadedServiceDescriptors.FirstOrDefault(a => a.FullServiceId == serviceIdentifier);

        /// <summary>
        /// Retrieve an instance of an Action
        /// </summary>
        /// <param name="serviceIdentifier">Service Identifier</param>
        /// <returns></returns>
        public ServiceInstance GetServiceInstance(string serviceIdentifier) =>
            (ServiceInstance)Activator.CreateInstance(GetServiceDescriptor(serviceIdentifier)?.ServiceInstanceType, new object[] { });
    }
}
