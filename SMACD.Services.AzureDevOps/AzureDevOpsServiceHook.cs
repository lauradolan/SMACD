using SMACD.Shared;
using SMACD.Shared.Attributes;
using SMACD.Shared.Plugins;
using SMACD.Shared.WorkspaceManagers;
using System.Threading.Tasks;

namespace SMACD.Services.AzureDevOps
{
    [ServiceHookMetadata("azdo", Name = "Azure DevOps Service")]
    public class AzureDevOpsServiceHook : ServiceHook
    {
        public override Task RegisterHooks()
        {
            ServiceHookManager.Instance.RegisterTaskHook(TaskServiceHookType.TaskCompleted, t => Instance_TaskCompleted(t));
            return Task.FromResult(0);
        }

        private void Instance_TaskCompleted(Task e)
        {
            if (e is Task<PluginResult>)
                CompletedPluginScan(((Task<PluginResult>)e).Result);
            else
                return; // task type unsupported
        }

        private void CompletedPluginScan(PluginResult result)
        {

        }
    }
}
