using Crayon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMACD.CLITool
{
    public static class TreeDump
    {
        private const string _cross = " ├─";
        private const string _corner = " └─";
        private const string _vertical = " │ ";
        private const string _space = "   ";

        private static Type[] ValueOnlyTypes = new[] { typeof(string), typeof(TimeSpan) };
        public static int MAX_WIDTH = 100;

        public static void Dump(object obj, string name = "", string indent = "", bool isLast = false)
        {
            var indentLen = indent.Length;
            indent = PrintNodeBase(indent, isLast);

            if (obj == null)
                Console.WriteLine(
                    Output.Underline().White().Text(name) +
                    Output.FromRgb(33, 33, 33).Text(" " + new string('.', 64 - indent.Length - name.Length) + " ") +
                    Output.Red().Dim().Text("<null>"));
            else if (obj.GetType().GetInterface(nameof(ICollection)) != null)
                DumpCollection((ICollection)obj, name, indent, isLast);
            else if (ValueOnlyTypes.Contains(obj.GetType()) || obj.GetType().GetProperties().Length == 0)
            {
                var textWidth = MAX_WIDTH - indent.Length; // remove tree bits
                textWidth -= name.Length; // remove name
                textWidth -= _vertical.Length; // new line
                textWidth -= 10; // remove 10 more chars

                var renderedValue = obj.ToString();
                if (obj is string) renderedValue = $"\"{renderedValue}\"";
                var strings = ((string)renderedValue).WordWrap(textWidth).Split('\n');

                var topLine = new string('.', MAX_WIDTH - textWidth - 1 + name.Length);
                var nextLines = new string(' ', MAX_WIDTH - textWidth - 1 + name.Length + 10);

                Console.Write(Output.Underline().White().Text(name) + " ");
                Console.WriteLine(Output.FromRgb(33, 33, 33).Text(topLine + " ") + strings.First());
                foreach (var str in strings.Skip(1))
                {
                    var fixedStr = str.Trim(' ', '\n', '\r', '\t');
                    if (string.IsNullOrEmpty(fixedStr)) continue;
                    for (int i = 0; i < indent.Length / 2 - 1; i++) Console.Write(_vertical);
                    //var wrapped = fixedStr.WordWrap(textWidth);
                    Console.WriteLine(nextLines + fixedStr);
                }

                //else
                //{
                //    Console.WriteLine(
                //        Output.Underline().White().Text(name) +
                //        Output.FromRgb(33, 33, 33).Text(" " + new string('.', 64 - indent.Length - name.Length) + " ") +
                //        renderedValue);
                //}
            }
            else
                DumpObject(obj, name, indent, isLast);
        }

        private static List<string> GetLines(string str, int maxWidth)
        {
            if (str.Length < maxWidth) return new List<string>() { str };

            var sentences = str.Split('\n').Except(new string[] { null });
            var lines = new List<string>();
            foreach (var sentence in sentences)
            {
                var tmpSentence = sentence;
                while (tmpSentence.Length > maxWidth)
                {
                    lines.Add(tmpSentence.Substring(0, maxWidth));
                    tmpSentence = tmpSentence.Substring(maxWidth);
                }
            }
            return lines;
        }

        private static void DumpCollection(ICollection obj, string name = "", string indent = "", bool isLast = false)
        {
            Console.WriteLine(name);
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
            Console.WriteLine(name);
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
