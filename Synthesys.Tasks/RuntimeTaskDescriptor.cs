using SMACD.Artifacts;
using Synthesys.SDK;
using Synthesys.SDK.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesys.Tasks
{
    public class RuntimeTaskDescriptor
    {
        public Task<List<ExtensionReport>> Task { get; set; }
        public Extension Extension { get; set; }
        public Artifact Artifact { get; set; }
    }
}
