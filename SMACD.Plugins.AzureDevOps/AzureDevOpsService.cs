using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using SMACD.SDK.Triggers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SMACD.Services.AzureDevOps
{
    [Extension("azuredevops",
        Name = "Azure DevOps Integration",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    // TODO: TRIGGERS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public class AzureDevOpsService : ReactionExtension
    {
        public string OrganizationUrl { get; set; }
        public string PersonalAccessToken { get; set; }

        private VssConnection Connection { get; set; }

        public AzureDevOpsService()
        {
            OrganizationUrl = "https://dev.azure.com/myorg";
            PersonalAccessToken = "pat";

            // Works around Assembly binding failure loading TypeConverter for these
            //   because the support Assemblies aren't in the probing path
            TypeDescriptor.AddAttributes(typeof(IdentityDescriptor), new TypeConverterAttribute(typeof(IdentityDescriptorConverter).FullName));
            TypeDescriptor.AddAttributes(typeof(SubjectDescriptor), new TypeConverterAttribute(typeof(SubjectDescriptorConverter).FullName));
        }

        //public override void Configure(Workspace.Workspace workspace)
        //{
        //    base.Configure(workspace);


        //    // TODO: Hook TaskFaulted to post error thread?
        //    Workspace.Tasks.TaskCompleted += (s, e) =>
        //    {
        //        // TODO: Get build agent env vars to determine the source of this
        //        //       (Probably a PR)

        //        CreatePullRequestThread(
        //            "SMACDSandbox",             // project
        //            "DSVW",                     // repository
        //            115,                        // pull request id
        //            e.Result.GetReportContent() // thread post content
        //        ).Wait();
        //    };

        //    Connection = new VssConnection(
        //        new Uri(OrganizationUrl), 
        //        new VssBasicCredential(string.Empty, PersonalAccessToken));
        //}

        private async Task CreatePullRequestThread(string projectId, string repositoryId, int pullRequestId, string content)
        {
            GitHttpClient cli = Connection.GetClient<GitHttpClient>();
            try
            {
                GitPullRequestCommentThread result = await cli.CreateThreadAsync(new GitPullRequestCommentThread()
                {
                    Comments = new List<Comment>()
                    {
                        new Comment()
                        {
                            Content = content,
                            CommentType = CommentType.System
                        }
                    }
                }, projectId, repositoryId, pullRequestId);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error posting thread");
            }
        }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            throw new NotImplementedException();
        }
    }
}
