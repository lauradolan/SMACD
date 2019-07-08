using Crayon;
using Serilog.Core;
using Serilog.Events;
using SMACD.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMACD.CLITool
{
    internal class TaskIdEnricher : ILogEventEnricher
    {
        #region Styling

        private static List<ConsoleColor> Colors { get; set; } = new List<ConsoleColor>()
        {
            ConsoleColor.Blue,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.Red,
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.DarkRed
        };

        private static int _colorIndex = 0;

        private static int ColorIndex
        {
            get
            {
                _colorIndex = (_colorIndex + 1) % Colors.Count;
                return _colorIndex;
            }
        }

        private static string Style(int idx, string s)
        {
            switch (idx)
            {
                case 0:
                    return Output.BrightBlue(s);

                case 1:
                    return Output.BrightCyan(s);

                case 2:
                    return Output.BrightMagenta(s);

                case 3:
                    return Output.BrightYellow(s);

                case 4:
                    return Output.BrightRed(s);

                case 5:
                    return Output.Blue(s);

                case 6:
                    return Output.Cyan(s);

                case 7:
                    return Output.Magenta(s);

                case 8:
                    return Output.Yellow(s);

                case 9:
                    return Output.Red(s);

                case -1:
                    return Output.White(s);
            }
            return s;
        }

        #endregion Styling

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (Task.CurrentId == null)
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TaskId", Output.Bold().Green().Text("#")));
            else if (Extensions.CurrentTask != null && Extensions.CurrentTask.Tag() != null)
            {
                var tagData = Shared.Extensions.CurrentTask.Tag();
                string str;
                if (tagData.Item1) // system thread
                    str = Output.White().Reversed().Text($"{tagData.Item2}");
                else if (!string.IsNullOrEmpty(tagData.Item2)) // named worker
                    str = Style(Task.CurrentId.GetValueOrDefault(-1) % Colors.Count, Output.Underline().Text(tagData.Item2));
                else // worker thread
                    str = Style(Task.CurrentId.GetValueOrDefault(-1) % Colors.Count, Output.Underline().Text($"WORKER#{Task.CurrentId})"));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TaskId", str));
            }
            else
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "TaskId", Style(Task.CurrentId.GetValueOrDefault(-1) % Colors.Count, $"WORK{Task.CurrentId}")));
        }
    }
}