using Microsoft.Extensions.Logging;
using System;

namespace SMACD.PluginHost
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();

        public static Action<object, string[], string> SerializeToFile { get; set; } = (a, b, c) => LogWhoops<object>();
        public static Func<object, string[], string> SerializeToString { get; set; } = (a, b) => LogWhoops<string>();
        public static Func<string, Type, object> DeserializeFromFile { get; set; } = (a, b) => LogWhoops<object>();
        public static Func<string, Type, object> DeserializeFromString { get; set; } = (a, b) => LogWhoops<object>();

        private static T LogWhoops<T>()
        {
            LogFactory.CreateLogger("Serializer")
                .LogCritical("Must specify Serialization strategy in {0}", typeof(Global).FullName);
            return default(T);
        }
    }
}
