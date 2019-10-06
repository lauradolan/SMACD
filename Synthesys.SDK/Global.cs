using Microsoft.Extensions.Logging;

namespace Synthesys.SDK
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}