using Microsoft.Extensions.Logging;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Targets;
using SMACD.Workspace.Tasks;
using SMACD.Workspace.Triggers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Toolbox of functions to control and manage Actions
    /// </summary>
    public class ActionToolbox : WorkspaceToolbox
    {
        internal ActionToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Get a list of ActionDescriptors that are triggered by a given source
        /// </summary>
        /// <param name="triggerSource">Trigger Source type</param>
        /// <param name="identifier">Trigger element identifier</param>
        /// <returns></returns>
        internal List<TriggerDescriptor> GetTriggeredActionDescriptors(TriggerSources triggerSource, string identifier) => 
            CurrentWorkspace.Libraries.LoadedActionDescriptors.SelectMany(a => a.TriggeredBy.Where(t => t.TriggerSource == triggerSource && t.TriggeringIdentifier == identifier)).ToList();

        /// <summary>
        /// Get a list of ActionInstances that are triggered by a given source
        /// </summary>
        /// <param name="triggerSource">Trigger Source type</param>
        /// <param name="identifier">Trigger element identifier</param>
        /// <param name="targets">Resource targets to pass</param>
        /// <returns></returns>
        public List<ActionInstance> GetTriggeredActions(TriggerSources triggerSource, string identifier, List<string> targets = null) => 
            GetTriggeredActionDescriptors(triggerSource, identifier).Select(t => GetActionInstance(t.ActionIdentifierCreated, t.DefaultOptionsOnCreation, targets)).ToList();

        /// <summary>
        /// Retrieve the ActionDescriptor for a given Action
        /// </summary>
        /// <param name="actionIdentifier">Action Identifier</param>
        /// <returns></returns>
        public ActionDescriptor GetActionDescriptor(string actionIdentifier) =>
            CurrentWorkspace.Libraries.LoadedActionDescriptors.FirstOrDefault(a => a.FullActionId == actionIdentifier);

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

            PropertyInfo[] actionInstancePropertyTypes = actionInstance.GetType().GetProperties();

            // -- Options --
            foreach (KeyValuePair<string, string> pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                PropertyInfo propertyIsConfigurable =
                    actionInstancePropertyTypes.FirstOrDefault(t => t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
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

            // -- Targets --
            // Resolve Target IDs
            IEnumerable<TargetDescriptor> targetDescriptors = targetIds.Select(t => CurrentWorkspace.Targets.GetTarget(t));

            // Iterate over each Target-eligible property defined on the ActionInstance
            foreach (PropertyInfo property in actionInstancePropertyTypes.Where(t =>
                typeof(TargetDescriptor).IsAssignableFrom(t.PropertyType)))
            {
                // Check if the property is a Collection (can take many targets)
                var isList = typeof(IList).IsAssignableFrom(property.PropertyType.GetType());
                Logger.LogDebug("Found Target eligible property {0} with discrete Type {1} (IList? {2})", property.Name,
                    property.PropertyType, isList);

                // Get Targets whose Types match the Target property
                List<TargetDescriptor> matchingTargetDescriptors = targetDescriptors.Where(r => 
                    property.PropertyType.IsAssignableFrom(r.GetType())).ToList();
                if (!matchingTargetDescriptors.Any())
                {
                    Logger.LogDebug("No TargetDescriptors are of a Type that matches Target-eligible property {0} (PropertyType: {1})",
                        property.Name, property.PropertyType);
                }
                else
                {
                    // Multiple TargetDescriptors, TargetProperty handles multiples
                    if (matchingTargetDescriptors.Count > 1 && isList)
                    {
                        for (int i = 0; i < matchingTargetDescriptors.Count; i++)
                        {
                            var collection = (IList)property.GetValue(actionInstance);
                            collection.Add(matchingTargetDescriptors);
                        }
                    }
                    // Multiple TargetDescriptors, TargetProperty handles single
                    else if (matchingTargetDescriptors.Count > 1 && !isList)
                    {
                        Logger.LogWarning("More than one matching TargetDescriptor but Property {0} (Type: {1}) is not an IList", property.Name, property.PropertyType);
                        property.SetValue(actionInstance, matchingTargetDescriptors.First());
                    }
                    // Single TargetDescriptor, TargetProperty handles multiples
                    else if (isList)
                    {
                        var collection = (IList)property.GetValue(actionInstance);
                        collection.Add(matchingTargetDescriptors);
                    }
                    // Single TargetDescriptor, TargetProperty handles single
                    else
                        property.SetValue(actionInstance, matchingTargetDescriptors.First());
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
