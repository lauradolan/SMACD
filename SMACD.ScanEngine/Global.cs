using Microsoft.Extensions.Logging;

namespace SMACD.ScanEngine
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}
