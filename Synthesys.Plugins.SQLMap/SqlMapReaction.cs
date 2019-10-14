using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.HostCommands;
using Synthesys.SDK.Triggers;
using Synthesys.Tasks;
using Synthesys.Tasks.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Synthesys.Plugins.SQLMap
{
    /// <summary>
    /// Creates SQLmap Tasks when URLArtifacts with GET/POST fields are found
    /// </summary>
    [Extension("sqlmap_field_reaction",
        Name = "SQLMap SQLi Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [TriggeredBy("**//{UrlRequestArtifact}*", AppTreeNodeEvents.IsCreated)]
    public class SqlMapReaction : ReactionExtension, ICanQueueTasks
    {
        private bool _useInLocalMode;
        public ITaskToolbox Tasks { get; set; }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            Logger.LogWarning($"FIRED SQLMAP REACTION - {trigger}");

            ArtifactTriggerDescriptor descriptor = trigger as ArtifactTriggerDescriptor;
            if (descriptor.Node is UrlRequestNode)
            {
                UrlRequestNode urlRequestArtifact = (UrlRequestNode)descriptor.Node;
                UrlNode urlArtifact = urlRequestArtifact.Parent as UrlNode;
                string url = urlArtifact.GetEntireUrl();

                if (urlRequestArtifact.Method == HttpMethod.Get)
                {
                    return Execute(url + "?" + string.Join('&', urlRequestArtifact.Fields.Keys.Select(k => $"{k}=1")));
                }
                else if (urlRequestArtifact.Method == HttpMethod.Post)
                {
                    return Execute(url + " " + string.Join(' ', urlRequestArtifact.Fields.Keys.Select(k => $"-p {k}")));
                }
            }
            return ExtensionReport.Blank();
        }

        public ExtensionReport Execute(string target)
        {
            string logFile;
            var Aggressive = false;

            NativeDirectoryEvidence nativePathArtifact = new NativeDirectoryEvidence("sqlmap-" + HashCode.Combine(target));
            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                if (DockerHostCommand.SupportsDocker())
                {
                    using var dockerCommand = new DockerHostCommand("alexandreoda/sqlmap:latest",
                        context,
                        "sqlmap",
                        $"--url={target}",
                        "--batch",
                        "--flush-session",
                        "--banner",
                        "--output-dir=/synthesys",
                        Aggressive ? "--level=5" : "--level=2",
                        Aggressive ? "--risk=3" : "--risk=1");
                    dockerCommand.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                    dockerCommand.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);

                    dockerCommand.Start().Wait();
                }
                else
                {
                    using var nativeCommand = new NativeHostCommand("sqlmap",
                        $"--url={target}",
                        "--batch",
                        "--flush-session",
                        "--banner",
                        $"--output-dir={context.Directory}",
                        Aggressive ? "--level=5" : "--level=2",
                        Aggressive ? "--risk=3" : "--risk=1");
                    nativeCommand.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                    nativeCommand.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);

                    nativeCommand.Start().Wait();
                }

                if (!File.Exists(context.DirectoryWithFile("log")))
                {
                    return ExtensionReport.Blank();
                }

                logFile = File.ReadAllText(context.DirectoryWithFile("log"));
            }

            SqlMapReport sqlMapReport = new SqlMapReport();
            System.Collections.Generic.List<string> issues = logFile.Split("---").Skip(1).ToList(); // first line is summary

            foreach (string issue in issues)
            {
                string[] lines = issue.Split('\n');
                SqlMapInjectionVector vector = new SqlMapInjectionVector();
                foreach (string line in lines)
                {
                    string name = line.Split(':')[0].Trim();
                    string value = line.Split(':')[1].Trim();

                    switch (name)
                    {
                        case "Parameter":
                            vector.Parameter = value;
                            break;
                        case "Type":
                            vector.Type = value;
                            break;
                        case "Title":
                            vector.Title = value;
                            break;
                        case "Payload":
                            vector.Payload = value;
                            break;
                    }
                }

                sqlMapReport.InjectionVectors.Add(vector);
            }

            ExtensionReport report = new ExtensionReport();
            report.SetExtensionSpecificReport(sqlMapReport);

            return report;
        }
    }
}
