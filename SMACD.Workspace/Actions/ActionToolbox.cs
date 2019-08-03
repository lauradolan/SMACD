using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions.Attributes;
using SMACD.Workspace.Targets;
using SMACD.Workspace.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Toolbox of functions to control and manage Actions
    /// </summary>
    public class ActionToolbox : WorkspaceToolbox
    {
        private List<ActionFileProvider> Providers { get; set; } = new List<ActionFileProvider>();
        private List<ActionDescriptor> Actions => Providers.SelectMany(p => p.ActionsProvided).ToList();

        /// <summary>
        /// List of Provider Libraries loaded
        /// </summary>
        public IReadOnlyList<ActionFileProvider> LoadedActionProviders => Providers;

        /// <summary>
        /// List of Actions available from loaded Provider Libraries
        /// </summary>
        public IReadOnlyList<ActionDescriptor> LoadedActionDescriptors => Actions;

        internal ActionToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Register Actions from a directory of ActionFileProvider libraries
        /// </summary>
        /// <param name="directory">Directory to search</param>
        /// <param name="fileMask">Mask of files to search for</param>
        public void RegisterActionsFromDirectory(string directory, string fileMask = "*.dll") => Directory.GetFiles(directory, fileMask).ToList()
            .ForEach(f => RegisterActionsFrom(f));

        /// <summary>
        /// Register Actions from a given ActionFileProvider library
        /// </summary>
        /// <param name="providerLibraryFile">Provider library</param>
        public void RegisterActionsFrom(string providerLibraryFile)
        {
            string resolvedName = new FileInfo(providerLibraryFile).FullName;
            if (Providers.Any(p => p.FileName == resolvedName))
            {
                Logger.LogWarning("Provider library already loaded!");
                return;
            }
            Providers.Add(new ActionFileProvider(resolvedName));
        }

        /// <summary>
        /// Get a list of ActionDescriptors that are triggered by a given source
        /// </summary>
        /// <param name="triggerSource">Trigger Source type</param>
        /// <param name="identifier">Trigger element identifier</param>
        /// <returns></returns>
        internal List<ActionTriggerDescriptor> GetTriggeredActionDescriptors(ActionTriggerSources triggerSource, string identifier) => 
            Actions.SelectMany(a => a.TriggeredBy.Where(t => t.TriggerSource == triggerSource && t.TriggeringIdentifier == identifier)).ToList();

        /// <summary>
        /// Get a list of ActionInstances that are triggered by a given source
        /// </summary>
        /// <param name="triggerSource">Trigger Source type</param>
        /// <param name="identifier">Trigger element identifier</param>
        /// <param name="targets">Resource targets to pass</param>
        /// <returns></returns>
        public List<ActionInstance> GetTriggeredActions(ActionTriggerSources triggerSource, string identifier, List<string> targets = null) => 
            GetTriggeredActionDescriptors(triggerSource, identifier).Select(t => GetActionInstance(t.ActionIdentifierCreated, t.DefaultOptionsOnCreation, targets)).ToList();

        /// <summary>
        /// Retrieve the ActionDescriptor for a given Action
        /// </summary>
        /// <param name="actionIdentifier">Action Identifier</param>
        /// <returns></returns>
        public ActionDescriptor GetActionDescriptor(string actionIdentifier) =>
            Actions.FirstOrDefault(a => a.FullActionId == actionIdentifier);

        /// <summary>
        /// Retrieve an instance of an Action
        /// </summary>
        /// <param name="actionIdentifier">Action Identifier</param>
        /// <returns></returns>
        public ActionInstance GetActionInstance(string actionIdentifier) =>
            (ActionInstance)Activator.CreateInstance(GetActionDescriptor(actionIdentifier)?.ActionInstanceType, new object[] { });

        /// <summary>
        /// Retrieve a configured instance of an Action
        /// </summary>
        /// <param name="actionIdentifier">Action Identifier</param>
        /// <param name="options">Options</param>
        /// <param name="targetIds">Target Identifiers</param>
        /// <returns></returns>
        public ActionInstance GetActionInstance(string actionIdentifier, Dictionary<string, string> options, List<string> targetIds)
        {
            ActionInstance actionInstance = GetActionInstance(actionIdentifier);
            if (options == null) options = new Dictionary<string, string>();
            if (targetIds == null) targetIds = new List<string>();

            PropertyInfo[] propertyTargets = actionInstance.GetType().GetProperties();
            foreach (KeyValuePair<string, string> pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                PropertyInfo propertyIsConfigurable =
                    propertyTargets.FirstOrDefault(t => t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
                                                        t.GetCustomAttribute<ConfigurableAttribute>() != null);
                if (propertyIsConfigurable != null)
                {
                    Logger.LogInformation("Found Configurable Property {0} (Type: {1}) -- Setting value {2}",
                        propertyIsConfigurable.Name, propertyIsConfigurable.PropertyType.Name, pair.Value);
                    propertyIsConfigurable.SetValue(this,
                        Convert.ChangeType(pair.Value, propertyIsConfigurable.PropertyType));
                }
                else
                {
                    Logger.LogWarning("Specified option '{0}' which is not understood by plugin '{1}'", pair.Key, actionIdentifier);
                }
            }

            IEnumerable<TargetDescriptor> targets = targetIds.Select(t => CurrentWorkspace.Targets.GetTarget(t));
            foreach (PropertyInfo property in propertyTargets.Where(t => typeof(TargetDescriptor).IsAssignableFrom(t.PropertyType)))
            {
                Logger.LogDebug("Found Resource eligible target {0} with discrete Type {1}", property.Name,
                    property.PropertyType);
                TargetDescriptor assigned = targets.FirstOrDefault(r => r.GetType() == property.PropertyType);
                if (assigned == null)
                {
                    Logger.LogDebug("Resource eligible target {0} did not match any provided Resources", property.Name);
                }
                else
                {
                    var assignedType = assigned.GetType();
                    var propType = property.PropertyType;
                    property.SetValue(actionInstance, assigned);
                }
            }

            var workspaceProperty = actionInstance.GetType().GetProperty("Workspace", BindingFlags.Instance | BindingFlags.NonPublic);
            var runtimeConfigProperty = actionInstance.GetType().GetProperty("RuntimeConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
            workspaceProperty.SetValue(actionInstance, CurrentWorkspace);
            runtimeConfigProperty.SetValue(actionInstance, new ResultProvidingTaskDescriptor()
            {
                ActionId = actionIdentifier,
                Options = options,
                TargetIds = targetIds
            });

            return actionInstance;
        }
    }
}
