using Crayon;
using SMACD.PluginHost.Extensions;
using System;

namespace SMACD.CLITool
{
    internal static class Extensions
    {
        public static void WriteTypeColoredText(this PluginTypes type, string text)
        {
            string outputText;
            switch (type)
            {
                case PluginTypes.Unknown:
                    outputText = text;
                    break;
                case PluginTypes.AttackTool:
                    outputText = Output.Red().Text(text);
                    break;
                case PluginTypes.Scorer:
                    outputText = Output.Green().Text(text);
                    break;
                case PluginTypes.Decision:
                    outputText = Output.Yellow().Text(text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.Write(outputText);
        }
    }
}
