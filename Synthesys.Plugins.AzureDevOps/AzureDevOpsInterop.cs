using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SMACD.AppTree;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesys.Plugins.AzureDevOps
{
    /// <summary>
    ///     Provides functions to work with objects in Azure DevOps
    /// </summary>
    public class AzureDevOpsInterop
    {
        /// <summary>
        ///     VSS Overarching Connection
        /// </summary>
        public VssConnection Connection { get; }

        /// <summary>
        ///     Repository Name from Build Agent environment
        /// </summary>
        public string RepositoryName => Environment.GetEnvironmentVariable("Build.Repository.ID");

        /// <summary>
        ///     Pull Request ID which prompted the build
        /// </summary>
        public int PullRequestId => Int32.Parse(Environment.GetEnvironmentVariable("System.PullRequest.PullRequestId"));
        
        /// <summary>
        ///     Project ID which prompted the build
        /// </summary>
        public string ProjectId => Environment.GetEnvironmentVariable("System.TeamProjectId");

        /// <summary>
        ///     Provides functions to work with objects in Azure DevOps
        /// </summary>
        /// <param name="connection">Authenticated VssConnection to Azure DevOps</param>
        public AzureDevOpsInterop(VssConnection connection) => Connection = connection;

        private ILogger Logger { get; } = SDK.Global.LogFactory.CreateLogger("AzureDevOpsInterop");

        public Task<GitPullRequestCommentThread> CreateThreadInPullRequest(string content) =>
            CreateThreadInPullRequest(ProjectId, RepositoryName, PullRequestId, content);

        public async Task<GitPullRequestCommentThread> CreateThreadInPullRequest(string projectId, string repositoryId, int pullRequestId,
            string content)
        {
            try
            {
                GitHttpClient cli = Connection.GetClient<GitHttpClient>();

                return await cli.CreateThreadAsync(new GitPullRequestCommentThread
                {
                    Comments = new List<Microsoft.TeamFoundation.SourceControl.WebApi.Comment>
                    {
                        new Microsoft.TeamFoundation.SourceControl.WebApi.Comment
                        {
                            Content = content,
                            CommentType = CommentType.System
                        }
                    }
                }, projectId, repositoryId, pullRequestId);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error creating thread in pull request");
            }
            return null;
        }

        public async Task<WorkItem> CreateWorkItem(string projectId, Vulnerability.RiskLevels riskLevel, string content)
        {
            try
            {
                WorkItemTrackingHttpClient cli = Connection.GetClient<WorkItemTrackingHttpClient>();

                var workItemType = "Task";
                JsonPatchDocument patchDocument = new JsonPatchDocument()
                {
                    PatchDocument.Title("Security Warning"),
                    PatchDocument.ReproSteps("Repro Steps"),
                    PatchDocument.Severity(riskLevel)
                };

                return await cli.CreateWorkItemAsync(patchDocument, Guid.Parse(projectId), workItemType, false, false);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error posting thread");
            }
            return null;
        }
    }
}
