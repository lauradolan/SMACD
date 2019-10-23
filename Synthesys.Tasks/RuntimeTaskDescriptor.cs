using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using System.Threading.Tasks;

namespace Synthesys.Tasks
{
    public class RuntimeTaskDescriptor
    {
        public Task<ExtensionReport> Task { get; set; }
        public Extension Extension { get; set; }
        public AppTreeNode Artifact { get; set; }
        public TriggerDescriptor Trigger { get; set; }
    }
}
