using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

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

        /// <summary>
        /// If this TargetDescriptor is approximate to a given TargetDescriptor
        /// 
        /// For example, if comparing two HttpTargets, they are approximate if the Hostname and Port match.
        /// </summary>
        /// <param name="scope">Scope to verify (bitflags)</param>
        /// <param name="descriptor">Descriptor to test</param>
        /// <returns></returns>
        public bool IsApproximateTo(ApproximationScopes scope, TargetDescriptor descriptor)
        {
            if (scope.HasFlag(ApproximationScopes.Host))
                if (!Check<IHasRemoteHost>(descriptor, (a, b) => 
                    a.RemoteHost == b.RemoteHost)) return false;
            if (scope.HasFlag(ApproximationScopes.Port))
                if (!Check<IHasPort>(descriptor, (a, b) => 
                    a.Port == b.Port)) return false;
            if (scope.HasFlag(ApproximationScopes.ResourceAccessMode))
                if (!Check<IHasResourceAccessMode>(descriptor, (a, b) => 
                    a.ResourceAccessMode == b.ResourceAccessMode)) return false;
            if (scope.HasFlag(ApproximationScopes.ResourceLocatorAddress))
                if (!Check<IHasResourceLocatorAddress>(descriptor, (a, b) => 
                    a.ResourceLocatorAddress == b.ResourceLocatorAddress)) return false;
            if (scope.HasFlag(ApproximationScopes.ParameterDictionary))
                if (!Check<IHasParameterDictionary>(descriptor, (a, b) => 
                    a.Parameters.Equals(b.Parameters))) return false;
            return true;
        }

        private bool Check<T>(TargetDescriptor descriptor, Func<T, T, bool> func) where T : class
        {
            if (this is T && descriptor is T) return func(this as T, descriptor as T);
            return false;
        }
    }

    [Flags]
    public enum ApproximationScopes
    {
        Host = 1,
        Port = 2,
        ResourceAccessMode = 4,
        ResourceLocatorAddress = 8,
        ParameterDictionary = 16
    }

    /// <summary>
    /// Manages Targets that are acted upon by Actions in the system
    /// </summary>
    public class TargetToolbox : WorkspaceToolbox
    {
        public event EventHandler<TargetDescriptor> TargetRegistered;

        private ConcurrentDictionary<string, TargetDescriptor> _targets { get; } = new ConcurrentDictionary<string, TargetDescriptor>();
        public ReadOnlyDictionary<string, TargetDescriptor> RegisteredTargets => new ReadOnlyDictionary<string, TargetDescriptor>(_targets);

        internal TargetToolbox(Workspace workspace) : base(workspace) { }

        /// <summary>
        /// Retrieve the Descriptor for a given Target Identifier
        /// </summary>
        /// <param name="targetId">Target Identifier</param>
        /// <returns></returns>
        public TargetDescriptor GetTarget(string targetId)
        {
            return _targets.ContainsKey(targetId) ? _targets[targetId] : null;
        }

        /// <summary>
        /// Register a Target
        /// </summary>
        /// <param name="targetDescriptor">Item inheriting from TargetDescriptor that describes the Target</param>
        public void RegisterTarget(TargetDescriptor targetDescriptor)
        {
            if (_targets.ContainsKey(targetDescriptor.TargetId))
            {
                Logger.LogWarning("Target ID already registered");
                return;
            }
            _targets.TryAdd(targetDescriptor.TargetId, targetDescriptor);
            TargetRegistered?.Invoke(this, targetDescriptor);
        }
    }
}
