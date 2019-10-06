using System.Collections.Generic;
using System.Threading.Tasks;
using CommandLine;
using Serilog.Events;

namespace Synthesys.Verbs
{
    public abstract class VerbBase
    {
        [Option('l', "loglevel", Default = 2, HelpText = "A value of 0-5, where lower values are more verbose",
            Required = false)]
        public LogEventLevel LogLevel { get; set; }

        [Option('z', "silence", Default = false,
            HelpText = "Suppress output styling (such as logo and section banners)", Required = false)]
        public bool Silent { get; set; }

        public abstract Task Execute();

        protected IList<string> SplitIntoLines(string targetString, int maxLength)
        {
            var str = (string) targetString.Clone();
            if (str.Length <= maxLength) return new List<string> {str};

            var result = new List<string>();
            var start = 0;
            while (start < str.Length - 1)
            {
                result.Add(str.Substring(start, maxLength));
                str = str.Substring(start + maxLength);
            }

            return result;
        }
    }
}