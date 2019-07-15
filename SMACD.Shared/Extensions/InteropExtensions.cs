using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Plugins;
using SMACD.Shared.Resources;
using YamlDotNet.Serialization;

namespace SMACD.Shared.Extensions
{
    public static class InteropExtensions
    {
        [ThreadStatic] public static Task CurrentTask;

        internal static T AddLoadedTagMappings<T>(this T builder) where T : BuilderSkeleton<T>
        {
            ResourceManager.GetKnownResourceHandlers()
                .ForEach(h => builder = builder.WithTagMapping("!" + h.Item1, h.Item2));
            return builder;
        }

        private static void WrappedLogCommand(int taskId, string hash, Action command)
        {
            ExecutionWrapper.Maps.TryAdd(Thread.CurrentThread.ManagedThreadId, taskId);
            command();
            ExecutionWrapper.Maps.Remove(Thread.CurrentThread.ManagedThreadId);
        }

        public static void TaskLogCritical(this ILogger logger, int taskId, string message,  params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogCritical(message, parameters));
        public static void TaskLogDebug(this ILogger logger, int taskId, string message, params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogDebug(message, parameters));
        public static void TaskLogError(this ILogger logger, int taskId, string message, params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogError(message, parameters));
        public static void TaskLogInformation(this ILogger logger, int taskId, string message, params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogInformation(message, parameters));
        public static void TaskLogTrace(this ILogger logger, int taskId, string message, params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogTrace(message, parameters));
        public static void TaskLogWarning(this ILogger logger, int taskId, string message, params object[] parameters) =>
            WrappedLogCommand(taskId, message.SHA1(), () => logger.LogWarning(message, parameters));
    }
}