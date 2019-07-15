using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;

namespace SMACD.Shared.Plugins.AttackTools
{
    /// <summary>
    ///     Represents a wrapper that can launch a scanner/tool and interpret its output to be summarized
    /// </summary>
    public abstract class AttackTool : Plugin
    {
        /// <summary>
        ///     Default Scorer to use with this Attack Tool
        /// </summary>
        public string DefaultScorer =>
            this.GetConfigAttribute<AttackToolMetadataAttribute, string>(a => a.DefaultScorer);

        /// <summary>
        ///     Resource types that can be processed by this plugin
        /// </summary>
        public IList<Type> ValidResourceTypes =>
            this.GetConfigAttribute<ValidResourcesAttribute, List<Type>>(a => a.Types);

        /// <summary>
        ///     Execute any tasks that the plugin requires to generate some output (i.e. run a scanner)
        /// </summary>
        /// <param name="pointer">Pointer to plugin and its configuration</param>
        /// <param name="workingDirectory">Working directory to store artifacts</param>
        /// <returns></returns>
        public abstract Task Execute(PluginPointerModel pointer, string workingDirectory);
    }
}