using Bogus;
using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Synthesys.Verbs
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
            Random random = new Random((int)DateTime.Now.Ticks);
            ServiceMapFile serviceMap = new ServiceMapFile();
            Logger.LogInformation("Creating 1-{0} of each element for a new Service Map", MaxOfEach);
            Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(featureId => new FeatureModel
            {
                Name = JargonGenerator.GenerateMultiPartJargon(),
                Description = new Faker().Lorem.Paragraph(2),
                Owners = Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(ownerId =>
                {
                    Person person = new Faker().Person;
                    return new OwnerPointerModel
                    {
                        Name = person.FullName,
                        Email = person.Email
                    };
                }).ToList(),

                UseCases = Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(useCaseId =>
                    new UseCaseModel
                    {
                        Name = JargonGenerator.GenerateMultiPartJargon(),
                        Description = new Faker().Lorem.Paragraph(2),
                        Owners = Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(ownerId =>
                        {
                            Person person = new Faker().Person;
                            return new OwnerPointerModel
                            {
                                Name = person.FullName,
                                Email = person.Email
                            };
                        }).ToList(),

                        AbuseCases = Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(
                            abuseCaseId =>
                                new AbuseCaseModel
                                {
                                    Name = JargonGenerator.GenerateMultiPartJargon(),
                                    Description = new Faker().Lorem.Paragraph(2),
                                    Owners = Enumerable.Range(0, random.Next(1, MaxOfEach)).Select(
                                        ownerId =>
                                        {
                                            Person person = new Faker().Person;
                                            return new OwnerPointerModel
                                            {
                                                Name = person.FullName,
                                                Email = person.Email
                                            };
                                        }).ToList(),

                                    Actions = Enumerable.Range(0, random.Next(1, MaxOfEach))
                                        .Select(
                                            pluginId => new ActionPointerModel
                                            {
                                                Action = "dummy",
                                                Options = new Dictionary<string, string>
                                                    {{"ConfigurationOption", "value"}},
                                                Target = "dummyResource"
                                            }).ToList()
                                }).ToList()
                    }).ToList()
            }).ToList().ForEach(f => serviceMap.Features.Add(f));

            serviceMap.Targets.Add(new HttpTargetModel
            {
                TargetId = "dummyResource",
                Url = "http://localhost"
            });

            Logger.LogDebug("Created all elements, generating Service Map file");
            ServiceMapFile.PutServiceMap(serviceMap, ServiceMap);
            Logger.LogInformation("Created new Service Map at {0}", ServiceMap);

            return Task.FromResult(0);
        }
    }
}