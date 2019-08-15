using Microsoft.Extensions.Logging;

namespace SMACD.SDK
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}
