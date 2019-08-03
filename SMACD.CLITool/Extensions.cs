using Crayon;
using SMACD.Workspace.Actions;
using System;

namespace SMACD.CLITool
{
    internal static class Extensions
    {
        public static void WriteTypeColoredText(this ActionRoles type, string text) =>
            Console.WriteLine(type.GetTypeColoredText(text));

        public static string GetTypeColoredText(this ActionRoles type, string text)
        {
            string outputText;
            switch (type)
            {
                case ActionRoles.Unknown:
                    outputText = text;
                    break;

                case ActionRoles.Producer:
                    outputText = Output.Red().Text(text);
                    break;

                case ActionRoles.Consumer:
                    outputText = Output.Green().Text(text);
                    break;

                case ActionRoles.Decider:
                    outputText = Output.Yellow().Text(text);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return outputText;
        }
    }
}