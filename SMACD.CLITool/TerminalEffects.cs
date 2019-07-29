using Crayon;
using System;
using System.Collections.Generic;

namespace SMACD.CLITool
{
    internal static class TerminalEffects
    {
        private static readonly string LOGO2 =
            "" + Environment.NewLine +
            " .d8888b.  888b     d888        d8888  .d8888b.  8888888b.  " + Environment.NewLine +
            "d88P  Y88b 8888b   d8888       d88888 d88P  Y88b 888  `Y88b " + Environment.NewLine +
            "Y88b.      88888b.d88888      d88P888 888    888 888    888 " + Environment.NewLine +
            " `Y888b.   888Y88888P888     d88P 888 888        888    888 " + Environment.NewLine +
            "    `Y88b. 888 Y888P 888    d88P  888 888        888    888 " + Environment.NewLine +
            "      `888 888  Y8P  888   d88P   888 888    888 888    888 " + Environment.NewLine +
            "Y88b  d88P 888   `   888  d8888888888 Y88b  d88P 888  .d88P " + Environment.NewLine +
            " `Y8888P`  888       888 d88P     888  `Y8888P`  8888888P`  ";

        private static string LOGO =
            "" + Environment.NewLine +
            "      _/_/_/  _/      _/    _/_/      _/_/_/  _/_/_/  " + Environment.NewLine +
            "   _/        _/_/  _/_/  _/    _/  _/        _/    _/ " + Environment.NewLine +
            "    _/_/    _/  _/  _/  _/_/_/_/  _/        _/    _/  " + Environment.NewLine +
            "       _/  _/      _/  _/    _/  _/        _/    _/   " + Environment.NewLine +
            "_/_/_/    _/      _/  _/    _/    _/_/_/  _/_/_/      ";

        internal static void DrawLogoBanner()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.WriteLine(Output.Red().Text(Center(LOGO2)));

            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(Output.Bold().Blue()
                .Text(Center("System Mapping & Architectural Concept Diagram CLI Tool")));

            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(new string(' ', Console.WindowWidth));

            Console.ResetColor();
            Console.WriteLine();
        }

        internal static void DrawSingleLineBanner(string text)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.WriteLine(Output.Blue().Bold().Text(Center(text)));

            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(new string(' ', Console.WindowWidth));

            Console.ResetColor();
            Console.WriteLine();
        }

        internal static string Center(string text)
        {
            var lines = new List<string>();
            foreach (var line in text.Split(Environment.NewLine))
            {
                var padLeft = (Console.WindowWidth - line.Length) / 2 + line.Length;
                lines.Add(line.PadLeft(padLeft).PadRight(Console.WindowWidth));
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}