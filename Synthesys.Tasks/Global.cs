using Microsoft.Extensions.Logging;

namespace Synthesys.Tasks
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}