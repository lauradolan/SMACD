using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
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
    [TriggeredBy("**//{UrlRequestArtifact}*", ArtifactTrigger.IsCreated)]
    public class SqlMapReaction : ReactionExtension, ICanQueueTasks
    {
        private bool _useInLocalMode;
        public ITaskToolbox Tasks { get; set; }

        public override bool ValidateEnvironmentReadiness()
        {
            ExecutionWrapper localTest = new ExecutionWrapper("sqlmap --help");
            localTest.Start().Wait();
            if (string.IsNullOrEmpty(localTest.StdErr))
            {
                _useInLocalMode = true;
            }

            return true;
        }

        public override ExtensionReport React(TriggerDescriptor trigger)
        {
            Logger.LogWarning($"FIRED SQLMAP REACTION - {trigger}");

            var descriptor = trigger as ArtifactTriggerDescriptor;
            if (descriptor.Artifact is UrlRequestArtifact)
            {
                var urlRequestArtifact = (UrlRequestArtifact)descriptor.Artifact;
                var urlArtifact = urlRequestArtifact.Parent as UrlArtifact;
                var url = urlArtifact.GetUrl();

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

            NativeDirectoryArtifact nativePathArtifact = new NativeDirectoryArtifact("sqlmap-" + HashCode.Combine(target));
            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                string dir = context.Directory;

                string baseCmd = "docker run -ti --rm --name sqlmap -v " + dir + ":/data alexandreoda/sqlmap";
                if (_useInLocalMode)
                {
                    baseCmd = "sqlmap";
                }
                
                var Aggressive = false; // todo: reaction config

                string cmd = baseCmd +
                          $" --url={target}" +
                          " --batch --flush-session --banner" +
                          (_useInLocalMode ? " --output-dir=" + dir : " --output-dir=/data") +
                          (Aggressive ? " --level=5 --risk=3" : " --level=2 --risk=1");

                ExecutionWrapper execution = new ExecutionWrapper(cmd);
                execution.Start().Wait();

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
