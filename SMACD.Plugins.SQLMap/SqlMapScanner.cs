using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Capabilities;
using SMACD.SDK.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SMACD.Plugins.SQLMap
{
    [Extension("sqlmap",
        Name = "SQLMap SQLi Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class SqlMapScanner : ActionExtension, IOperateOnUrl
    {
        [Configurable] public bool Aggressive { get; set; } = false;

        public UrlArtifact Url { get; set; }

        private bool _useInLocalMode = false;
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

        public override ExtensionReport Act()
        {
            string logFile;

            NativeDirectoryArtifact nativePathArtifact = new NativeDirectoryArtifact("sqlmap-" + Url.GetUrl());
            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                string dir = context.Directory;

                string baseCmd = "docker run -ti --rm --name sqlmap -v " + dir + ":/data alexandreoda/sqlmap";
                if (_useInLocalMode)
                {
                    baseCmd = "sqlmap";
                }

                string cmd = baseCmd +
                        $" --url={Url.GetUrl()}" +
                        " --batch --flush-session --banner" +
                        (_useInLocalMode ? " --output-dir=" + dir :
                            " --output-dir=/data") +
                        (Aggressive ?
                            " --level=5 --risk=3" :
                            " --level=2 --risk=1");

                ExecutionWrapper execution = new ExecutionWrapper(cmd);
                execution.Start().Wait();

                if (!File.Exists(context.DirectoryWithFile("log")))
                {
                    return new SqlMapReport();
                }

                logFile = File.ReadAllText(context.DirectoryWithFile("log"));
            }

            SqlMapReport report = new SqlMapReport();
            List<string> issues = logFile.Split("---").Skip(1).ToList(); // first line is summary

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
                report.InjectionVectors.Add(vector);
            }
            return report;
        }
    }

    public class SqlMapInjectionVector
    {
        public string Parameter { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Payload { get; set; }
    }
}
