using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Describes a Target that can be acted upon in the system
    /// </summary>
    public abstract class TargetDescriptor
    {
        /// <summary>
        /// Target identifier
        /// </summary>
        public string TargetId { get; set; }
    }

    /// <summary>
    /// Manages Targets that are acted upon by Actions in the system
    /// </summary>
    public class TargetToolbox : WorkspaceToolbox
    {
        private ConcurrentDictionary<string, TargetDescriptor> Targets { get; } = new ConcurrentDictionary<string, TargetDescriptor>();

        internal TargetToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Retrieve the Descriptor for a given Target Identifier
        /// </summary>
        /// <param name="targetId">Target Identifier</param>
        /// <returns></returns>
        public TargetDescriptor GetTarget(string targetId)
        {
            return Targets.ContainsKey(targetId) ? Targets[targetId] : null;
        }

        /// <summary>
        /// Register a Target
        /// </summary>
        /// <param name="targetDescriptor">Item inheriting from TargetDescriptor that describes the Target</param>
        public void RegisterTarget(TargetDescriptor targetDescriptor)
        {
            if (Targets.ContainsKey(targetDescriptor.TargetId))
            {
                Logger.LogWarning("Target ID already registered");
                return;
            }
            Targets.TryAdd(targetDescriptor.TargetId, targetDescriptor);
        }
    }
}
