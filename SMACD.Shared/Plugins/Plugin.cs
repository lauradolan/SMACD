using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Plugins
{
    public abstract class Plugin
    {
        /// <summary>
        ///     Name of the plugin
        /// </summary>
        public string Name => this.GetConfigAttribute<MetadataAttribute, string>(a => a.Name);

        /// <summary>
        ///     Identifier to be used in descriptor files
        /// </summary>
        public string Identifier => this.GetConfigAttribute<MetadataAttribute, string>(a => a.Identifier);

        /// <summary>
        ///     Options that can be specified to modify the default behavior of the plugin
        /// </summary>
        public IList<ConfigurableOptionAttribute> ConfigurableOptions => this
            .GetConfigAttributes<ConfigurableOptionAttribute, ConfigurableOptionAttribute>(a => a).ToList();

        protected ILogger Logger { get; set; }

        protected Plugin()
        {
            Logger = Workspace.LogFactory.CreateLogger(GetType());
        }

        protected string GetOption(IDictionary<string, string> options, string option)
        {
            var opts = GetOptions(options);
            if (!opts.ContainsKey(option)) return null;
            return opts[option];
        }

        protected Dictionary<string, string> GetOptions(IDictionary<string, string> options)
        {
            var coalescedOptions =
                new Dictionary<string, string>(ConfigurableOptions.ToDictionary(k => k.OptionName, v => v.Default));
            foreach (var item in options)
                coalescedOptions[item.Key] = item.Value;
            return coalescedOptions;
        }
    }
}
