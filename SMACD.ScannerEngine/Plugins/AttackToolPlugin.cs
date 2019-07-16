using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;

namespace SMACD.ScannerEngine.Plugins
{
    public abstract class AttackToolPlugin : Plugin
    {
        /// <summary>
        /// Resource[s] this Attack Tool will act on
        /// </summary>
        public IList<Resource> Resources { get; set; } = new List<Resource>();
        public abstract Task Execute();

        internal override Plugin Validate()
        {
            var permittedResources = AllowResourceTypeAttribute.Get(GetType());
            var invalidResources = Resources.Where(r =>
                permittedResources.Any(permitted => r.GetType().IsInstanceOfType(permitted))).ToList();
            if (invalidResources.Any())
                throw new Exception("Invalid resources: " + string.Join(", ", invalidResources));

            return base.Validate();
        }
    }
}