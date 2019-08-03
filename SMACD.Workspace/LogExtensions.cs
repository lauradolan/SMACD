using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using System;
using System.Text;
using System.Threading;

namespace SMACD.Workspace
{
    public static class LogExtensions
    {
        private static void WrappedLogCommand(int taskId, string hash, Action command)
        {
            ExecutionWrapper.Maps.TryAdd(Thread.CurrentThread.ManagedThreadId, taskId);
            command();
            ExecutionWrapper.Maps.TryRemove(Thread.CurrentThread.ManagedThreadId, out int dummy);
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

        /// <summary>
        ///     Calculate SHA1 hash of a string (not cryptographically safe operation!)
        /// </summary>
        /// <param name="str">String to hash</param>
        /// <returns></returns>
        public static string SHA1(this string str)
        {
            try
            {
                byte[] hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
                StringBuilder ret = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    ret.Append(hash[i].ToString("x2"));
                }

                return ret.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}