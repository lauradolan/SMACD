using SMACD.Shared.Attributes;
using System.Threading.Tasks;

namespace SMACD.Shared
{
    public abstract class ServiceHook
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => this.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Name);

        /// <summary>
        /// Identifier to be used in descriptor files
        /// </summary>
        public string Identifier => this.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Identifier);

        public virtual Task RegisterHooks() => Task.FromResult(0);
    }
}
