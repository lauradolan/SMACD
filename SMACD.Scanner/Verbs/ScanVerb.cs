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

        public override async Task Execute()
        {
            // Serialize/Deserialize "rootArtifact"
            // Need to call pre save/load functions to sew up tree pointers

            // --------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "wks_workingdir", DateTime.Now.ToUniversalTime().ToString("u").Replace(" ", string.Empty).Replace(':', '-'));
            }

            if (!Directory.Exists(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }

            // Create scan session
            Session session = new Session();

            //if (File.Exists(Path.Combine(WorkingDirectory, "workspace.yaml")))
            //    workspace.Load(Path.Combine(WorkingDirectory, "workspace.yaml"));

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

            // Copy original Service Map to Working Directory
            File.Copy(
                ServiceMap,
                Path.Combine(WorkingDirectory, "input.yaml"));

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

            //using (var sw = new StreamWriter(Path.Combine(WorkingDirectory, "workspace.yaml")))
            //    workspace.Save(sw.BaseStream);

            //workspace.Unbind(); // unbind manually

            //var json = JsonConvert.SerializeObject(workspace.Reports);
            //var outputFile = Path.Combine(WorkingDirectory,
            //    "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            //using (var sw = new StreamWriter(outputFile))
            //{
            //    sw.WriteLine(json);
            //}

            if (!Silent)
            {
                TreeDump.Dump(results.ToArray(), "Generated Data Reports", isLast: true);
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

            //Console.WriteLine("Average score: {0}", results.Average(r => r.AdjustedScore));
            //Console.WriteLine("Summed score: {0}", results.Sum(r => r.AdjustedScore));
            //Console.WriteLine("Median score: {0}", results.OrderBy(r => r.AdjustedScore).ElementAt(results.Count / 2));

            //Logger.LogInformation("Report serialized to {0}", outputFile);

            //if (Threshold.HasValue)
            //{
            //    Logger.LogDebug("Checking threshold");
            //    if (Threshold > results.Average(r => r.AdjustedScore))
            //    {
            //        Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
            //            results.Average(r => r.AdjustedScore));
            //        Environment.Exit(-1);
            //    }
            //    else
            //    {
            //        Logger.LogDebug("Passed");
            //        Environment.Exit(0);
            //    }
            //}
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