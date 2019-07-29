using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crayon;
using SMACD.Data;
using SMACD.PluginHost.Resources;

namespace SMACD.CLITool
{
    public class TreeRenderer
    {
        public delegate void DataModelCallback<in T>(string indent, bool isLast, T feature) where T : IModel;

        private const string _cross = " ├─";
        private const string _corner = " └─";
        private const string _vertical = " │ ";
        private const string _space = "   ";

        private static readonly int VALIDATION_QUESTION_FULL_WIDTH = (int) (Console.WindowWidth * 0.8d);

        public int TestsExecuted { get; set; }
        public int TestsPassed { get; set; }
        public int TestsFailed { get; set; }

        public event DataModelCallback<FeatureModel> AfterFeatureDrawn;
        public event DataModelCallback<UseCaseModel> AfterUseCaseDrawn;
        public event DataModelCallback<AbuseCaseModel> AfterAbuseCaseDrawn;
        public event DataModelCallback<PluginPointerModel> AfterPluginPointerDrawn;
        public event DataModelCallback<ResourceModel> AfterResourceDrawn;

        public string WriteExecutedTest(string testName, Func<bool?> testToRun, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            Console.Write(
                Output.BrightWhite(testName) +
                Output.FromRgb(33, 33, 33)
                    .Text(" " + new string('.', VALIDATION_QUESTION_FULL_WIDTH - testName.Length - indent.Length - 2) +
                          " "));
            var result = testToRun();
            TestsExecuted++;
            if (result.HasValue)
            {
                if (result.Value)
                {
                    TestsPassed++;
                    Console.WriteLine(Output.BrightGreen("Yes"));
                }
                else
                {
                    TestsFailed++;
                    Output.BrightRed("No");
                }
            }
            else
            {
                Console.WriteLine(Output.Yellow("N/A"));
            }

            return indent;
        }

        public void PrintNode(FeatureModel model, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            Console.WriteLine(Output.BrightBlue(model.Name));
            AfterFeatureDrawn?.Invoke(indent, isLast, model);
            foreach (var useCase in model.UseCases)
                PrintNode(useCase, indent, model.UseCases.Last() == useCase);
        }

        public void PrintNode(UseCaseModel model, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            Console.WriteLine(Output.BrightGreen(model.Name));
            AfterUseCaseDrawn?.Invoke(indent, isLast, model);
            foreach (var abuseCase in model.AbuseCases)
                PrintNode(abuseCase, indent, model.AbuseCases.Last() == abuseCase);
        }

        public void PrintNode(AbuseCaseModel model, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            Console.WriteLine(Output.BrightRed(model.Name));
            AfterAbuseCaseDrawn?.Invoke(indent, isLast, model);
            foreach (var pluginPointer in model.PluginPointers)
                PrintNode(pluginPointer, indent, model.PluginPointers.Last() == pluginPointer);
        }

        public void PrintNode(PluginPointerModel model, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            if (model.Resource != null)
            {
                var resource = ResourceManager.Instance.GetById(model.Resource.ResourceId);
                Console.WriteLine(Output.BrightMagenta(model.Plugin) + " -> " +
                                  Output.BrightYellow(resource.ToString()));
            }
            else
            {
                Console.WriteLine(Output.BrightMagenta(model.Plugin) + " -> " + Output.Yellow("<No ResourceModel>"));
            }

            AfterPluginPointerDrawn?.Invoke(indent, isLast, model);
        }

        public void PrintNode(ResourceModel model, string indent = "", bool isLast = false)
        {
            indent = PrintNodeBase(indent, isLast);
            Console.WriteLine(model.ResourceId);
            AfterResourceDrawn?.Invoke(indent, isLast, model);
        }

        public string PrintNodeBase(string indent, bool isLast)
        {
            Console.Write(indent);
            if (isLast)
            {
                Console.Write(_corner);
                indent += _space;
            }
            else
            {
                Console.Write(_cross);
                indent += _vertical;
            }

            return indent;
        }
    }
}