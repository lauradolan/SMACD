using Crayon;
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace SMACD.Scanner.Helpers
{
    public static class TreeDump
    {
        private const string _cross = " ├─";
        private const string _corner = " └─";
        private const string _vertical = " │ ";
        private const string _space = "   ";

        private static Type[] ValueOnlyTypes = new[] { typeof(string), typeof(TimeSpan), typeof(DateTime) };
        public static int MAX_WIDTH = Console.WindowWidth - 10;
        public static int forcedPadding = 10;

        private enum LineTypes { Normal, Collection, Minimal }
        private static string GetLine(LineTypes type, string name, int dotsLength, string value)
        {
            string str = string.Empty;
            if (type == LineTypes.Minimal)
                str += new string(' ', name.Length + 1 + dotsLength);
            else
            {
                IOutput baseColor;
                // Numeric names  (indexes)  rendered purple
                if (Int32.TryParse(name, out var dummy)) baseColor = Output.Magenta();
                else baseColor = Output.White();

                if (type == LineTypes.Collection) // underline collection names
                    str += baseColor.Underline().Text(name) + " ";
                else
                    str += baseColor.Text(name) + " ";

                str += Output.FromRgb(33, 33, 33).Text(new string('.', dotsLength)); 
            }

            if (value == null)
                str += Output.Red().Text("<null>");
            else
                str += value;
            return str;
        }

        public static void Dump(object obj, string name = "", string indent = "", bool isLast = false)
        {
            var indentLen = indent.Length;
            indent = PrintNodeBase(indent, isLast);

            // WRITE NULL CASE
            if (obj == null)
                Console.WriteLine(GetLine(LineTypes.Normal, name, MAX_WIDTH - indentLen - name.Length - 1 - "<null>".Length, null));

            // RECURSE INTO COLLECTION CASE
            else if (obj.GetType().GetInterface(nameof(ICollection)) != null)
                DumpCollection((ICollection)obj, name, indent, isLast);

            // VALUE
            else if (ValueOnlyTypes.Contains(obj.GetType()) || obj.GetType().GetProperties().Length == 0)
            {
                var deadSpace = indentLen + 1 + name.Length + 1;

                var renderedValue = obj.ToString();
                if (obj is string) renderedValue = $"\"{renderedValue}\""; // format output value

                // LONG STRINGS
                if (deadSpace + renderedValue.Length + 2 > MAX_WIDTH)
                {
                    var strings = ((string)renderedValue).WordWrap(
                        MAX_WIDTH - deadSpace - 2 - forcedPadding).Split('\n');

                    var c = 0;
                    foreach (var str in strings.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim('\r','\n','\t',' ')))
                    {
                        var middleWidth = MAX_WIDTH - indentLen - $" {name} ".Length - str.Length + 1;
                        if (c++ == 0)
                        {
                            // <indent> <name> {...} <value>
                            Console.WriteLine(GetLine(LineTypes.Normal, name, middleWidth, str));
                        }
                        else
                        {
                            Console.Write(indent);
                            // <indent> <name> {   } <value>
                            Console.WriteLine(GetLine(LineTypes.Minimal, name, middleWidth, str));
                        }
                    }
                }
                // NORMAL STRINGS
                else
                {
                    var middleWidth = MAX_WIDTH - indentLen - $" {name} ".Length - renderedValue.Length + 1;

                    var totalUsedSpace = deadSpace + renderedValue.Length;
                    var freeSpace = MAX_WIDTH - totalUsedSpace;
                    var dots = new string('.', freeSpace);
                    Console.WriteLine(
                        GetLine(LineTypes.Normal, name, middleWidth, renderedValue));
                }
            }

            // RECURSE INTO OBJECT
            else
                DumpObject(obj, name, indent, isLast);
        }

        private static void DumpCollection(ICollection obj, string name = "", string indent = "", bool isLast = false)
        {
            Console.WriteLine(GetLine(LineTypes.Collection, name, 0, ""));
            //Console.WriteLine(Output.Underline().White().Text(name));
            var c = 0; var l = obj.Count;
            foreach (var item in obj)
            {
                Dump(
                    item,
                    c.ToString(),
                    indent,
                    (c++ >= l - 1));
            }
        }

        private static void DumpObject(object obj, string name = "", string indent = "", bool isLast = false)
        {
            Console.WriteLine(GetLine(LineTypes.Normal, name, 0, ""));
            //Console.WriteLine(Output.Underline().White().Text(name));
            var properties = obj.GetType().GetProperties();
            foreach (var property in properties)
            {
                Dump(
                    property.GetValue(obj),
                    property.Name,
                    indent,
                    properties.Last() == property);
            }
        }

        private static string PrintNodeBase(string indent, bool isLast)
        {
            // Writes indent base and tree line
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
            Console.Write(" ");

            return indent;
        }
    }

    public static class WordWrapExtension
    {
        /// <summary>
        /// Word wraps the given text to fit within the specified width.
        /// </summary>
        /// <param name="text">Text to be word wrapped</param>
        /// <param name="width">Width, in characters, to which the text
        /// should be word wrapped</param>
        /// <returns>The modified text</returns>
        public static string WordWrap(this string text, int width)
        {
            int pos, next;
            StringBuilder sb = new StringBuilder();

            // Lucidity check
            if (width < 1)
                return text;

            // Parse each line of text
            for (pos = 0; pos < text.Length; pos = next)
            {
                // Find end of line
                int eol = text.IndexOf(Environment.NewLine, pos);
                if (eol == -1)
                    next = eol = text.Length;
                else
                    next = eol + Environment.NewLine.Length;

                // Copy this line of text, breaking into smaller lines as needed
                if (eol > pos)
                {
                    do
                    {
                        int len = eol - pos;
                        if (len > width)
                            len = BreakLine(text, pos, width);
                        sb.Append(text, pos, len);
                        sb.Append(Environment.NewLine);

                        // Trim whitespace following break
                        pos += len;
                        while (pos < eol && Char.IsWhiteSpace(text[pos]))
                            pos++;
                    } while (eol > pos);
                }
                else sb.Append(Environment.NewLine); // Empty line
            }
            return sb.ToString();
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private static int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            int i = max;
            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
                i--;

            // If no whitespace found, break at maximum length
            if (i < 0)
                return max;

            // Find start of whitespace
            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
                i--;

            // Return length of text before whitespace
            return i + 1;
        }
    }
}