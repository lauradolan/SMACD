using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;

namespace SMACD.ScannerEngine.Resources
{
    /// <summary>
    ///     Manages Resources and their fingerprints
    /// </summary>
    public class ResourceManager
    {
        public delegate bool ResourceCollisionDelegate(Resource originalResource, Resource newResource);

        private static readonly Lazy<ResourceManager>
            _instance = new Lazy<ResourceManager>(() => new ResourceManager());

        private ResourceManager()
        {
        }

        public static ResourceManager Instance => _instance.Value;

        private ConcurrentDictionary<string, Resource> ResourcesById { get; } =
            new ConcurrentDictionary<string, Resource>();

        private ConcurrentDictionary<string, Resource> ResourcesByFingerprint { get; } =
            new ConcurrentDictionary<string, Resource>();

        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("ResourceManager");

        /// <summary>
        ///     Fired when a Resource is successfully registered
        /// </summary>
        public event EventHandler<Resource> ResourceRegistered;

        /// <summary>
        ///     Fired when a Resource has a collision with another Resource's ID
        /// </summary>
        public event ResourceCollisionDelegate ResourceIdCollision;

        /// <summary>
        ///     Clear the Resource cache
        /// </summary>
        internal void Clear()
        {
            Logger.LogDebug("Clearing Resource cache");
            ResourcesByFingerprint.Clear();
            ResourcesById.Clear();
        }

        /// <summary>
        ///     Fired when a Resource has a collision with another Resource's fingerprint
        /// </summary>
        public event ResourceCollisionDelegate ResourceFingerprintCollision;

        /// <summary>
        ///     Get a list of known resource handler tags and types
        /// </summary>
        /// <returns>Builder with tag mappings</returns>
        internal static List<Tuple<string, Type>> GetKnownResourceHandlers()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Resource)))
                .Select(type =>
                {
                    var attr = PluginMetadataAttribute.Get(type);
                    if (attr == null) return null;
                    return Tuple.Create(attr.Identifier, type);
                })).Except(new Tuple<string, Type>[] {null}).ToList();
        }

        public bool ContainsId(string resourceId)
        {
            return ResourcesById.ContainsKey(resourceId);
        }

        public bool ContainsFingerprint(string fingerprint)
        {
            return ResourcesByFingerprint.ContainsKey(fingerprint);
        }

        public bool ContainsPointer(ResourcePointerModel pointer)
        {
            return ResourcesById.ContainsKey(pointer.ResourceId);
        }

        public Resource GetById(string resourceId)
        {
            if (!ContainsId(resourceId))
            {
                Logger.LogCritical("Resource ID '{0}' not found", resourceId);
                throw new Exception($"Resource ID '{resourceId}' not found");
            }

            return ResourcesById[resourceId];
        }

        public Resource GetByFingerprint(string fingerprint)
        {
            if (!ContainsFingerprint(fingerprint))
            {
                Logger.LogCritical("Resource fingerprint '{0}' not found", fingerprint);
                throw new Exception($"Resource fingerprint '{fingerprint}' not found");
            }

            return ResourcesByFingerprint[fingerprint];
        }

        public Resource GetByPointer(ResourcePointerModel pointer)
        {
            if (!ContainsPointer(pointer))
            {
                Logger.LogCritical("Resource pointer '{0}' not found", pointer);
                throw new Exception($"Resource pointer '{pointer}' not found");
            }

            return ResourcesById[pointer.ResourceId]?.GetWithParameters(pointer.ResourceParameters);
        }

        public Resource Search(Predicate<Resource> selector)
        {
            return ResourcesById.Values.FirstOrDefault(r => selector(r));
        }

        /// <summary>
        ///     Update the hash code cache
        /// </summary>
        public void UpdateCache()
        {
            Logger.LogDebug("=== EXPIRING AND REGENERATING RESOURCE HASH CACHE FOR {0} ITEMS ===",
                ResourcesByFingerprint.Count);
            ResourcesByFingerprint.Clear();
            Parallel.ForEach(ResourcesById, r => ResourcesByFingerprint.TryAdd(r.Value.Fingerprint(), r.Value));
        }

        /// <summary>
        ///     Register a new Resource
        /// </summary>
        /// <param name="resource">Resource to register</param>
        /// <returns></returns>
        public Resource Register(Resource resource)
        {
            if (resource.ResourceId == null)
                resource.ResourceId = RandomExtensions.RandomName();

            if (ContainsId(resource.ResourceId)) // Contains resource by Id
            {
                var existing = GetById(resource.ResourceId);
                if (!(ResourceIdCollision?.Invoke(existing, resource)).GetValueOrDefault(false)) 
                    // if FALSE or NULL, break -- otherwise override stop behavior
                    return null;
                existing.Instances.Add(resource);
                Logger.LogDebug("Attempted to register resource ID {0} which already exists -- added to existing element {1}", resource.ResourceId, existing.ResourceId);
            }

            var resourceFingerprint = resource.Fingerprint();
            if (ContainsFingerprint(resourceFingerprint)) // Contains resource by fingerprint
            {
                var existing = GetByFingerprint(resource.Fingerprint());
                if (!(ResourceIdCollision?.Invoke(existing, resource)).GetValueOrDefault(false))
                    // if FALSE or NULL, break -- otherwise override stop behavior
                    return null;
                existing.Instances.Add(resource);
                Logger.LogDebug(
                    "Attempted to register resource {0} fingerprint {1} which already exists at resource {2} -- adding to instance list",
                    resource.ResourceId, resourceFingerprint, existing.ResourceId);
            }

            ResourcesById.TryAdd(resource.ResourceId, resource);
            ResourcesByFingerprint.TryAdd(resourceFingerprint, resource);

            Logger.LogDebug("Registered Resource {0} with fingerprint {1}", resource.ResourceId,
                resourceFingerprint);
            ResourceRegistered?.Invoke(this, resource);
            return resource;
        }
    }
}