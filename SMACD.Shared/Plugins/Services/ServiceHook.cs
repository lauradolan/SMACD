using System.Threading.Tasks;
using SMACD.Shared.Attributes;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Plugins.Services
{
    public abstract class ServiceHook
    {
        /// <summary>
        ///     Name of the plugin
        /// </summary>
        public string Name => this.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Name);

        /// <summary>
        ///     Identifier to be used in descriptor files
        /// </summary>
        public string Identifier => this.GetConfigAttribute<ServiceHookMetadataAttribute, string>(a => a.Identifier);

        public virtual Task RegisterHooks()
        {
            return Task.FromResult(0);
        }
    }
}