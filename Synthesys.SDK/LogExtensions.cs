﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Synthesys.SDK
{
    /// <summary>
    ///     Extensions to the log function to allow a binding between external processes and their encapsulating threads
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        ///     Managed to unmanaged thread ID maps for logging enrichment extension
        /// </summary>
        public static Dictionary<int, int> Maps { get; } = new Dictionary<int, int>();

        private static void WrappedLogCommand(int taskId, Action command)
        {
            Maps.TryAdd(Thread.CurrentThread.ManagedThreadId, taskId);
            command();
            Maps.Remove(Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        ///     Log a message at the CRITICAL level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogCritical(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogCritical(message, parameters));
        }

        /// <summary>
        ///     Log a message at the DEBUG level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogDebug(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogDebug(message, parameters));
        }

        /// <summary>
        ///     Log a message at the ERROR level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogError(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogError(message, parameters));
        }

        /// <summary>
        ///     Log a message at the INFORMATION level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogInformation(this ILogger logger, int taskId, string message,
            params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogInformation(message, parameters));
        }

        /// <summary>
        ///     Log a message at the TRACE level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogTrace(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogTrace(message, parameters));
        }

        /// <summary>
        ///     Log a message at the WARNING level
        /// </summary>
        /// <param name="logger">Logger to invoke</param>
        /// <param name="taskId">Task ID to correlate to thread</param>
        /// <param name="message">Message to log</param>
        /// <param name="parameters">Parameters for message template</param>
        public static void TaskLogWarning(this ILogger logger, int taskId, string message, params object[] parameters)
        {
            WrappedLogCommand(taskId, () => logger.LogWarning(message, parameters));
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