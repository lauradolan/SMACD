﻿using SMACD.AppTree;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.HostCommands;
using System.IO;
using System.Linq;

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
        /// <summary>
        ///     If <c>TRUE</c>, SQLmap will run at Level 5/Risk Level 3 instead of the default Level 2/Risk Level 1
        /// </summary>
        [Configurable]
        public bool Aggressive { get; set; } = false;

        /// <summary>
        ///     URL being scanned
        /// </summary>
        public UrlNode Url { get; set; }
        
        public override ExtensionReport Act()
        {
            string logFile;

            NativeDirectoryEvidence nativePathArtifact = new NativeDirectoryEvidence("sqlmap-" + Url.GetEntireUrl());
            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                if (DockerHostCommand.SupportsDocker())
                {
                    using var dockerCommand = new DockerHostCommand("alexandreoda/sqlmap:latest",
                        context,
                        "sqlmap",
                        $"--url={Url.GetEntireUrl()}",
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
                        $"--url={Url.GetEntireUrl()}",
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