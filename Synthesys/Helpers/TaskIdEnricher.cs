using Crayon;
using Serilog.Core;
using Serilog.Events;
using Synthesys.SDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesys.Helpers
{
    internal class TaskIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (Task.CurrentId == null || LogExtensions.Maps.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            {
                if (LogExtensions.Maps.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    int owner = LogExtensions.Maps[Thread.CurrentThread.ManagedThreadId];
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TaskId",
                        Style(owner % Colors.Count, Output.Underline().Text("WORK" + owner))));
                }
                else
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TaskId",
                        Output.Bold().Green().Text("#MAIN#")));
                }
            }
            else
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "TaskId", Style(Task.CurrentId.GetValueOrDefault(-1) % Colors.Count, $"WORK{Task.CurrentId}")));
            }
        }

        #region Styling

        private static List<ConsoleColor> Colors { get; } = new List<ConsoleColor>
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
    }
}