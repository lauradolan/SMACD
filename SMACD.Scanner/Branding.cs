using Colorful;
using System;
using System.Drawing;
using System.IO;
using Console = Colorful.Console;

namespace SMACD.Scanner
{
    public static class Branding
    {
        public static void DrawBanner() =>
            DrawBanner(new Color[5]
            {
                Color.FromArgb(0x3c, 0xbb, 0x00),
                Color.FromArgb(0x3b, 0xca, 0x00),
                Color.FromArgb(0x3a, 0xdd, 0x00),
                Color.FromArgb(0x39, 0xf1, 0x00),
                Color.FromArgb(0x38, 0xff, 0x00)
            });
        private static void DrawBanner(Color[] gradient)
        {
            var bannerFormat =
                "" + Environment.NewLine +
                " {0}" + Environment.NewLine +
                " {1}  {5}" + Environment.NewLine +
                " {2}  {6} {7} {8} {9}" + Environment.NewLine +
                " {3}  {10}" + Environment.NewLine +
                " {4}";
            Formatter[] bannerLines = new Formatter[]
            {
                new Formatter("┏━┓┏┳┓┏━┓┏━╸╺┳┓ ╭┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉╌╌┄┄┈┈", gradient[0]), // 
                new Formatter("┃  ┃┃┃┃ ┃┃   ┃┃ ┇ ", gradient[1]),
                new Formatter("┗━┓┃┃┃┣━┫┃   ┃┃ ┇ ", gradient[2]),
                new Formatter("  ┃┃┃┃┃ ┃┃   ┃┃ ┇ ", gradient[3]),
                new Formatter("┗━┛╹ ╹╹ ╹┗━╸╺┻┛ ╰"+new string('┉', Console.WindowWidth-25)+"╌╌┄┄┈┈", gradient[4]),
                new Formatter("Service Map Scanning Tool", Color.White),
                new Formatter("Version", Color.White),
                new Formatter("1.0.0", Color.LightGreen), // var
                new Formatter(string.IsNullOrEmpty(Hash) ? "" : "build", Color.White),
                new Formatter(Hash, Color.LightGreen), // var
                new Formatter("github.com/anthturner/SMACD", Color.LightBlue)
            };

            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLineFormatted(bannerFormat, Color.White, bannerLines);
            Console.WriteLine();
            Console.ResetColor();
        }

        public static string Hash
        {
            get
            {
                var dir = GitDirectoryLocation;
                if (string.IsNullOrEmpty(dir))
                    return string.Empty;

                var headFile = File.ReadAllText(Path.Combine(dir, ".git", "HEAD"));
                if (headFile.StartsWith("ref:"))
                {
                    var refLocation = headFile.Substring("ref: ".Length).Trim('\r', '\n', '\t');
                    var hash = File.ReadAllText(Path.Combine(dir, ".git", refLocation));
                    hash = hash.Substring(0, 8);
                    return hash;
                }
                else
                    throw new Exception("Unexpected file format for HEAD");
            }
        }


        public static string GitDirectoryLocation
        {
            get
            {
                var dir = Directory.GetCurrentDirectory();

                while (!Directory.Exists(Path.Combine(dir, ".git")))
                {
                    var parent = Directory.GetParent(dir);
                    if (parent == null || parent.FullName == dir)
                        return string.Empty;
                    dir = Directory.GetParent(dir).FullName;
                }
                return dir;
            }
        }
    }
}
