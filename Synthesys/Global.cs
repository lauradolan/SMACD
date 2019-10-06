using Microsoft.Extensions.Logging;

namespace Synthesys
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}