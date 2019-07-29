using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Plugins;

namespace SMACD.PluginHost.Extensions
{
    public static class InteropExtensions
    {
        private static void WrappedLogCommand(int taskId, string hash, Action command)
        {
            ExecutionWrapper.Maps.TryAdd(Thread.CurrentThread.ManagedThreadId, taskId);
            command();
            ExecutionWrapper.Maps.TryRemove(Thread.CurrentThread.ManagedThreadId, out var dummy);
        }

        public static void TaskLogCritical(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogCritical(message, parameters));
        }

        public static void TaskLogDebug(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogDebug(message, parameters));
        }

        public static void TaskLogError(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogError(message, parameters));
        }

        public static void TaskLogInformation(this ILogger logger, int taskId, string message,
            params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogInformation(message, parameters));
        }

        public static void TaskLogTrace(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogTrace(message, parameters));
        }

        public static void TaskLogWarning(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogWarning(message, parameters));
        }
    }
}