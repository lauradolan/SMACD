using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.SQLMap
{
    /// <summary>
    /// sqlmap is an open source penetration testing tool that automates the process of detecting and exploiting SQL injection flaws and taking over of database servers. It comes with a powerful detection engine, many niche features for the ultimate penetration tester and a broad range of switches lasting from database fingerprinting, over data fetching from the database, to accessing the underlying file system and executing commands on the operating system via out-of-band connections.
    /// </summary>
    [Extension("sqlmap",
        Name = "SQLMap SQLi Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class SqlMapScanner : ActionExtension, IOperateOnUrl
    {
        /// <summary>
        /// If <c>TRUE</c>, SQLmap will run at Level 5/Risk Level 3 instead of the default Level 2/Risk Level 1
        /// </summary>
        [Configurable] public bool Aggressive { get; set; } = false;

        /// <summary>
        /// URL being scanned
        /// </summary>
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

    /// <summary>
    /// A way to request a page which results in the ability to read/write otherwise protected data, execute code, etc.
    /// </summary>
    public class SqlMapInjectionVector
    {
        /// <summary>
        /// Parameter that is injectable
        /// </summary>
        public string Parameter { get; set; }

        /// <summary>
        /// Type of injection
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Title of injection
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Injection payload
        /// </summary>
        public string Payload { get; set; }
    }
}
