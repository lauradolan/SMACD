using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SMACD.Shared;
using SMACD.Shared.Plugins;
using SMACD.Shared.WorkspaceManagers;

namespace SMACD.Services.AzureDevOps
{
    public class AzureDevOpsService : ServiceHook
    {
        private bool IsBuildingPullRequest =>
            Environment.GetEnvironmentVariable("System.PullRequest.PullRequestId") != null;

        public override Task RegisterHooks()
        {
            ServiceHookManager.Instance.RegisterTaskHook(TaskServiceHookType.TaskCompleted, task =>
            {
                var errorOccurred = task.IsFaulted;
                var pluginResult = ((Task<PluginResult>) task).Result;

                var status = new GitPullRequestStatus
                {
                    State = errorOccurred ? GitStatusState.Succeeded : GitStatusState.Failed
                };

                if (IsBuildingPullRequest)
                {
                    VssConnection connection = null;
                    var repositoryId = Guid.NewGuid();
                    AddToCurrentPullRequest(connection, repositoryId, status);
                }
            });

            return Task.FromResult(0);
        }

        private static VssConnection GetConnection(Uri orgUri, string personalAccessToken)
        {
            return new VssConnection(orgUri, new VssBasicCredential(string.Empty, personalAccessToken));
        }

        public static Task<GitPullRequestStatus> AddToCurrentPullRequest(VssConnection connection, Guid repositoryId,
            GitPullRequestStatus status)
        {
            return AddToPullRequest(
                connection, repositoryId,
                int.Parse(Environment.GetEnvironmentVariable("System.PullRequest.PullRequestId")),
                status);
        }

        public static async Task<GitPullRequestStatus> AddToPullRequest(VssConnection connection, Guid repositoryId,
            int pullRequestId, GitPullRequestStatus status)
        {
            var gitClient = connection.GetClient<GitHttpClient>();
            return await gitClient.CreatePullRequestStatusAsync(
                status,
                repositoryId,
                pullRequestId);
        }

        public static async Task<WorkItem> CreateWorkItem(VssConnection connection, JsonPatchDocument document,
            Guid project, string type)
        {
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            return await witClient.CreateWorkItemAsync(document, project, type);
        }
    }
}