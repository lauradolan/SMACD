using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Data;
using SMACD.Data.Resources;
using SMACD.ScanEngine;
using SMACD.Scanner.Helpers;
using SMACD.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms.Dynamic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Scanner.Verbs
{
    [Verb("scan", HelpText = "Run scan using plugins specified in a Service Map")]
    public class ScanVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Location of the Service Map", Required = true)]
        public string ServiceMap { get; set; }

        [Option('d', "workingdirectory", HelpText = "Working directory of Workspace")]
        public string WorkingDirectory { get; set; }

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = SMACD.ScanEngine.Global.LogFactory.CreateLogger<ScanVerb>();

        private bool workingDirectoryProvided;
        public override async Task Execute()
        {
            Logger.LogDebug("Starting ExtensionLibrary search");
            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Plugins"),
                "SMACD.Plugins.*.dll");

            // --------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                workingDirectoryProvided = true;
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "wks_workingdir", DateTime.Now.ToUniversalTime().ToString("u").Replace(" ", string.Empty).Replace(':', '-'));
            }

            Session session = null;
            if (File.Exists(Path.Combine(WorkingDirectory, "session")))
            {
                Logger.LogDebug("Session file exists, opening", WorkingDirectory);
                using (var stream = new FileStream(Path.Combine(WorkingDirectory, "session"), FileMode.Open, FileAccess.Read))
                {
                    session = new Session(stream);
                }
            }
            else
            {
                Logger.LogDebug("Session file not found in Working Directory {0}, creating new Session", WorkingDirectory);
                if (!Directory.Exists(WorkingDirectory))
                    Directory.CreateDirectory(WorkingDirectory);

                session = new Session();
                session.ServiceMapYaml = File.ReadAllText(ServiceMap);
            }

            // Import Service Map
            ServiceMapFile serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            // Register Targets from Resources
            foreach (TargetModel resourceModel in serviceMap.Targets)
            {
                if (resourceModel is HttpResourceModel)
                {
                    HttpResourceModel http = resourceModel as HttpResourceModel;
                    Uri uri = new Uri(http.Url);
                    List<string> pieces = uri.AbsolutePath.Split('/').ToList();
                    if (session.Artifacts[uri.Host].ChildNames.Contains(uri.Port.ToString()))
                    {
                        // TODO: Do this better.
                        ServicePortArtifact original = session.Artifacts[uri.Host][uri.Port];
                        session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact()
                        {
                            ServiceName = original.ServiceName,
                            ServiceBanner = original.ServiceBanner
                        };
                    }
                    else
                    {
                        session.Artifacts[uri.Host][uri.Port] = new HttpServicePortArtifact();
                    }

                    UrlArtifact pathTip = GeneratePathArtifacts(((HttpServicePortArtifact)session.Artifacts[uri.Host][uri.Port]), uri.AbsolutePath, http.Method);

                    pathTip.Requests.Add(new UrlRequestArtifact()
                    {
                        Parent = pathTip,
                        Fields = new ObservableDictionary<string, string>(http.Fields),
                        Headers = new ObservableDictionary<string, string>(http.Headers)
                    });
                }
                if (resourceModel is SocketPortResourceModel)
                {
                    SocketPortResourceModel socket = resourceModel as SocketPortResourceModel;
                    session.Artifacts[socket.Hostname][$"{socket.Protocol}/{socket.Port}"].ServiceName = "";
                }
            }

            List<Task<ExtensionReport>> generatedTasks = new List<Task<ExtensionReport>>();
            foreach (FeatureModel feature in serviceMap.Features)
            {
                foreach (UseCaseModel useCase in feature.UseCases)
                {
                    foreach (AbuseCaseModel abuseCase in useCase.AbuseCases)
                    {
                        foreach (ActionPointerModel pluginPointer in abuseCase.Actions)
                        {
                            TargetModel target = serviceMap.Targets.FirstOrDefault(t => t.TargetId == pluginPointer.Target.TargetId);

                            Artifact artifact = null;
                            if (target is HttpResourceModel)
                            {
                                Uri uri = new Uri(((HttpResourceModel)target).Url);
                                artifact = session.Artifacts[uri.Host][uri.Port];
                            }
                            else if (target is SocketPortResourceModel)
                            {
                                artifact = session.Artifacts
                                    [((SocketPortResourceModel)target).Hostname]
                                    [((SocketPortResourceModel)target).Port];
                            }

                            generatedTasks.Add(session.Tasks.Enqueue(new TaskDescriptor()
                            {
                                ActionId = pluginPointer.Action,
                                Options = pluginPointer.Parameters,
                                ArtifactRoot = artifact,
                                ProjectPointer = new ProjectPointer()
                                {
                                    Feature = feature,
                                    UseCase = useCase,
                                    AbuseCase = abuseCase
                                }
                            }));
                        }
                    }
                }
            }

            while (session.Tasks.IsRunning)
            {
                System.Threading.Thread.Sleep(500);
            }

            var results = generatedTasks.Select(t => t.Result.FinalizeReport());

            using (var stream = new FileStream(Path.Combine(WorkingDirectory, "session"), FileMode.OpenOrCreate, FileAccess.Write))
            {
                session.Export(stream);
            }

            if (!Silent)
            {
                TreeDump.Dump(results.Where(r => !(r is ErroredExtensionReport)).ToArray(), "Generated Data Reports", isLast: true);
            }

            if (!Silent)
            {
                session.Artifacts.Disconnect();
                try
                {
                    TreeDump.Dump(session.Artifacts, "Artifact Correlation Tree", isLast: true);
                }
                catch (Exception ex)
                { }
            }

            if (!Silent || workingDirectoryProvided)
                Logger.LogInformation("Report serialized to {0}", Path.Combine(WorkingDirectory, "session"));

            if (!Silent)
            {
                Console.WriteLine("Average score: {0}", results.Average(r => r.AdjustedScore));
                Console.WriteLine("Summed score: {0}", results.Sum(r => r.AdjustedScore));
                Console.WriteLine("Median score: {0}", results.OrderBy(r => r.AdjustedScore).ElementAt(results.Count() / 2).AdjustedScore);
            }

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > results.Average(r => r.AdjustedScore))
                {
                    Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
                        results.Average(r => r.AdjustedScore));
                    Console.WriteLine(results.Average(r => r.AdjustedScore));
                    Environment.Exit(-1);
                }
                else
                {
                    Logger.LogDebug("Passed");
                    Environment.Exit(0);
                }
            }
        }

        private UrlArtifact GeneratePathArtifacts(HttpServicePortArtifact httpService, string url, string method)
        {
            List<string> pieces = url.Split('/').ToList();
            pieces.RemoveAll(p => string.IsNullOrEmpty(p));
            UrlArtifact artifact = httpService["/"];
            foreach (string piece in pieces)
            {
                if (pieces.Last() == piece)
                {
                    if (method.ToUpper() == "GET")
                    {
                        artifact[piece].Method = HttpMethod.Get;
                    }
                    else if (method.ToUpper() == "POST")
                    {
                        artifact[piece].Method = HttpMethod.Post;
                    }
                    else if (method.ToUpper() == "PUT")
                    {
                        artifact[piece].Method = HttpMethod.Put;
                    }
                    else if (method.ToUpper() == "DELETE")
                    {
                        artifact[piece].Method = HttpMethod.Delete;
                    }
                    else if (method.ToUpper() == "HEAD")
                    {
                        artifact[piece].Method = HttpMethod.Head;
                    }
                    else if (method.ToUpper() == "TRACE")
                    {
                        artifact[piece].Method = HttpMethod.Trace;
                    }
                }
                artifact = artifact[piece];
            }
            return artifact;
        }


        //private void RunBeforeSave(Artifact artifact)
        //{
        //    artifact.Root = null;
        //    artifact.Parent = null;
        //    foreach (var child in artifact.Children)
        //        RunBeforeSave(child);
        //}

        //private void RunAfterLoad(Artifact artifactAtGeneration, Artifact root = null, Artifact parent = null)
        //{
        //    if (parent != null)
        //        artifactAtGeneration.Parent = parent;
        //    else
        //        artifactAtGeneration.Root = artifactAtGeneration;

        //    artifactAtGeneration.Root = root;

        //    foreach (var child in artifactAtGeneration.Children)
        //        RunAfterLoad(child, artifactAtGeneration.Root, artifactAtGeneration);
        //}

        private static string Serialize<T>(T obj)
        {
            return new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithTagMapping("!http", typeof(SMACD.Data.Resources.HttpResourceModel))
                .WithTagMapping("!raw", typeof(SMACD.Data.Resources.SocketPortResourceModel))
                .Build()
                .Serialize(obj);
        }

        private static T Deserialize<T>(string yaml)
        {
            return new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithTagMapping("!http", typeof(SMACD.Data.Resources.HttpResourceModel))
                .WithTagMapping("!raw", typeof(SMACD.Data.Resources.SocketPortResourceModel))
                .Build()
                .Deserialize<T>(yaml);
        }
    }
}