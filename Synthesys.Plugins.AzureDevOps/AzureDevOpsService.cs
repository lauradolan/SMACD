﻿using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Synthesys.Plugins.AzureDevOps
{
    [Extension("azuredevops",
        Name = "Azure DevOps Integration",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    // TODO: TRIGGERS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public class AzureDevOpsService : ReactionExtension
    {
        public AzureDevOpsService()
        {
            OrganizationUrl = "https://dev.azure.com/myorg";
            PersonalAccessToken = "pat";

            // Works around Assembly binding failure loading TypeConverter for these
            //   because the support Assemblies aren't in the probing path
            TypeDescriptor.AddAttributes(typeof(IdentityDescriptor),
                new TypeConverterAttribute(typeof(IdentityDescriptorConverter).FullName));
            TypeDescriptor.AddAttributes(typeof(SubjectDescriptor),
                new TypeConverterAttribute(typeof(SubjectDescriptorConverter).FullName));
        }

        public string OrganizationUrl { get; set; }
        public string PersonalAccessToken { get; set; }

        private VssConnection Connection { get; set; }

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

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            throw new NotImplementedException();
        }
    }
}