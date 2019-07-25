using Bogus;
using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.PluginHost;
using SMACD.PluginHost.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("generate", HelpText = "Generate a barebones sample Service Map")]
    public class GenerateVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        [Option('m', "max", HelpText = "Maximum number of elements of each generation to create", Default = 3)]
        public int MaxOfEach { get; set; }

        private static ILogger<GenerateVerb> Logger { get; } = Global.LogFactory.CreateLogger<GenerateVerb>();

        public override Task Execute()
        {
            var serviceMap = new ServiceMapFile();
            Logger.LogInformation("Creating 1-{0} of each element for a new Service Map", MaxOfEach);
            Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(featureId => new FeatureModel
            {
                Name = JargonGenerator.GenerateMultiPartJargon(),
                Description = new Faker().Lorem.Paragraph(2),
                Owners = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(ownerId =>
                {
                    var person = new Faker().Person;
                    return new OwnerPointerModel
                    {
                        Name = person.FullName,
                        Email = person.Email
                    };
                }).ToList(),

                UseCases = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(useCaseId =>
                    new UseCaseModel
                    {
                        Name = JargonGenerator.GenerateMultiPartJargon(),
                        Description = new Faker().Lorem.Paragraph(2),
                        Owners = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(ownerId =>
                        {
                            var person = new Faker().Person;
                            return new OwnerPointerModel
                            {
                                Name = person.FullName,
                                Email = person.Email
                            };
                        }).ToList(),

                        AbuseCases = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(
                            abuseCaseId =>
                                new AbuseCaseModel
                                {
                                    Name = JargonGenerator.GenerateMultiPartJargon(),
                                    Description = new Faker().Lorem.Paragraph(2),
                                    Owners = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach)).Select(
                                        ownerId =>
                                        {
                                            var person = new Faker().Person;
                                            return new OwnerPointerModel
                                            {
                                                Name = person.FullName,
                                                Email = person.Email
                                            };
                                        }).ToList(),

                                    PluginPointers = Enumerable.Range(0, RandomExtensions.Random.Next(1, MaxOfEach))
                                        .Select(
                                            pluginId => new PluginPointerModel
                                            {
                                                Plugin = "dummy",
                                                PluginParameters = new Dictionary<string, string>
                                                    {{"parameter", "value"}},
                                                Resource = new ResourcePointerModel { ResourceId = "dummyResource" }
                                            }).ToList()
                                }).ToList()
                    }).ToList()
            }).ToList().ForEach(f => serviceMap.Features.Add(f));
            new List<object>
            {
                new
                {
                    ResourceId = "dummyResource",
                    Url = "http://localhost"
                }
            }.ForEach(r => serviceMap.Resources.Add((ResourceModel)r));

            Logger.LogDebug("Created all elements, generating Service Map file");
            ServiceMapFile.PutServiceMap(serviceMap, ServiceMap);
            Logger.LogInformation("Created new Service Map at {0}", ServiceMap);

            return Task.FromResult(0);
        }
    }
}