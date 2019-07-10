using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Shared;
using SMACD.Shared.Data;
using SMACD.Shared.Resources;

namespace SMACD.CLITool.Verbs
{
    [Verb("generate", HelpText = "Generate a barebones sample Service Map")]
    public class GenerateVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        [Option('m', "max", HelpText = "Maximum number of elements of each generation to create", Default = 3)]
        public int MaxOfEach { get; set; }

        private static ILogger<GenerateVerb> Logger { get; } = Extensions.LogFactory.CreateLogger<GenerateVerb>();

        public override Task Execute()
        {
            Workspace.Instance.CreateEphemeral();

            Logger.LogInformation("Creating 1-{0} of each element for a new Service Map", MaxOfEach);
            Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(featureId => new FeatureModel
            {
                Name = new Faker().Lorem.Sentence(),
                Description = new Faker().Lorem.Paragraph(2),
                Owners = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(ownerId =>
                {
                    var person = new Faker().Person;
                    return new OwnerPointerModel
                    {
                        Name = person.FullName,
                        Email = person.Email
                    };
                }).ToList(),

                UseCases = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(useCaseId =>
                    new UseCaseModel
                    {
                        Name = new Faker().Lorem.Sentence(),
                        Description = new Faker().Lorem.Paragraph(2),
                        Owners = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(ownerId =>
                        {
                            var person = new Faker().Person;
                            return new OwnerPointerModel
                            {
                                Name = person.FullName,
                                Email = person.Email
                            };
                        }).ToList(),

                        AbuseCases = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(abuseCaseId =>
                            new AbuseCaseModel
                            {
                                Name = new Faker().Lorem.Sentence(),
                                Description = new Faker().Lorem.Paragraph(2),
                                Owners = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(ownerId =>
                                {
                                    var person = new Faker().Person;
                                    return new OwnerPointerModel
                                    {
                                        Name = person.FullName,
                                        Email = person.Email
                                    };
                                }).ToList(),

                                PluginPointers = Enumerable.Range(0, Extensions.Random.Next(1, MaxOfEach)).Select(
                                    pluginId => new PluginPointerModel
                                    {
                                        Plugin = "dummy",
                                        PluginParameters = new Dictionary<string, string> {{"parameter", "value"}},
                                        Resource = new ResourcePointerModel {ResourceId = "dummyResource"}
                                    }).ToList()
                            }).ToList()
                    }).ToList()
            }).ToList().ForEach(f => Workspace.Instance.ServiceMap.Features.Add(f));

            new List<Resource>
            {
                new HttpResource
                {
                    ResourceId = "dummyResource",
                    Url = "http://localhost"
                }
            }.ForEach(r => Workspace.Instance.ServiceMap.Resources.Add(r));

            Logger.LogInformation("Created all elements, generating Service Map file");
            Workspace.PutServiceMap(Workspace.Instance.ServiceMap, ServiceMap);
            Logger.LogInformation("Created new Service Map at {0}", ServiceMap);

            return Task.FromResult(0);
        }
    }
}