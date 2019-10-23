using Microsoft.Extensions.Logging;

namespace Synthesys.SDK
{
    /// <summary>
    ///     Global entities for SDK
    /// </summary>
    public static class Global
    {
        /// <summary>
        ///     Log factory for elements in SDK
        /// </summary>
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();
    }
}