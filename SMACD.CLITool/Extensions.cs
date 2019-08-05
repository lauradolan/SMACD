using Crayon;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Libraries;
using System;

namespace SMACD.CLITool
{
    internal static class Extensions
    {
        public static void WriteTypeColoredText(this ExtensionRoles type, string text) =>
            Console.WriteLine(type.GetTypeColoredText(text));

        public static string GetTypeColoredText(this ExtensionRoles type, string text)
        {
            string outputText;
            switch (type)
            {
                case ExtensionRoles.Unknown:
                    outputText = text;
                    break;

                case ExtensionRoles.Producer:
                    outputText = Output.Red().Text(text);
                    break;

                case ExtensionRoles.Consumer:
                    outputText = Output.Green().Text(text);
                    break;

                case ExtensionRoles.Decider:
                    outputText = Output.Yellow().Text(text);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return outputText;
        }
    }
}