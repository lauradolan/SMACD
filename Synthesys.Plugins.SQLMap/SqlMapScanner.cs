using System.IO;
using System.Linq;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.SQLMap
{
    /// <summary>
    ///     sqlmap is an open source penetration testing tool that automates the process of detecting and exploiting SQL
    ///     injection flaws and taking over of database servers. It comes with a powerful detection engine, many niche features
    ///     for the ultimate penetration tester and a broad range of switches lasting from database fingerprinting, over data
    ///     fetching from the database, to accessing the underlying file system and executing commands on the operating system
    ///     via out-of-band connections.
    /// </summary>
    [Extension("sqlmap",
        Name = "SQLMap SQLi Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class SqlMapScanner : ActionExtension, IOperateOnUrl
    {
        private bool _useInLocalMode;

        /// <summary>
        ///     If <c>TRUE</c>, SQLmap will run at Level 5/Risk Level 3 instead of the default Level 2/Risk Level 1
        /// </summary>
        [Configurable]
        public bool Aggressive { get; set; } = false;

        /// <summary>
        ///     URL being scanned
        /// </summary>
        public UrlArtifact Url { get; set; }

        public override bool ValidateEnvironmentReadiness()
        {
            var localTest = new ExecutionWrapper("sqlmap --help");
            localTest.Start().Wait();
            if (string.IsNullOrEmpty(localTest.StdErr)) _useInLocalMode = true;

            return true;
        }

        public override ExtensionReport Act()
        {
            string logFile;

            var nativePathArtifact = new NativeDirectoryArtifact("sqlmap-" + Url.GetUrl());
            using (var context = nativePathArtifact.GetContext())
            {
                var dir = context.Directory;

                var baseCmd = "docker run -ti --rm --name sqlmap -v " + dir + ":/data alexandreoda/sqlmap";
                if (_useInLocalMode) baseCmd = "sqlmap";

                var cmd = baseCmd +
                          $" --url={Url.GetUrl()}" +
                          " --batch --flush-session --banner" +
                          (_useInLocalMode ? " --output-dir=" + dir : " --output-dir=/data") +
                          (Aggressive ? " --level=5 --risk=3" : " --level=2 --risk=1");

                var execution = new ExecutionWrapper(cmd);
                execution.Start().Wait();

                if (!File.Exists(context.DirectoryWithFile("log"))) return new SqlMapReport();

                logFile = File.ReadAllText(context.DirectoryWithFile("log"));
            }

            var report = new SqlMapReport();
            var issues = logFile.Split("---").Skip(1).ToList(); // first line is summary

            foreach (var issue in issues)
            {
                var lines = issue.Split('\n');
                var vector = new SqlMapInjectionVector();
                foreach (var line in lines)
                {
                    var name = line.Split(':')[0].Trim();
                    var value = line.Split(':')[1].Trim();

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
}