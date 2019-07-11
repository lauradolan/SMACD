using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.Plugins.Extensions
{
    public static class TagDataExtension
    {
        private static readonly Dictionary<WeakReference<Task>, Tuple<bool, string>> _taskNames =
            new Dictionary<WeakReference<Task>, Tuple<bool, string>>();

        /// <summary>
        ///     Tag a Task with some additional information
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="tagData">Tag information</param>
        /// <param name="isSystem">If the source is a system-level source</param>
        public static void Tag(this Task task, string tagData, bool isSystem = false)
        {
            if (task == null) return;
            var weakReference = ContainsTask(task);
            if (weakReference == null)
                weakReference = new WeakReference<Task>(task);
            _taskNames[weakReference] = Tuple.Create(isSystem, tagData);
        }

        /// <summary>
        ///     Retrieve the data associated with a Task
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns></returns>
        public static Tuple<bool, string> Tag(this Task task)
        {
            var weakReference = ContainsTask(task);
            if (weakReference == null) return null;
            return _taskNames[weakReference];
        }

        private static WeakReference<Task> ContainsTask(Task task)
        {
            foreach (var kvp in _taskNames.ToList())
                if (!kvp.Key.TryGetTarget(out var taskFromReference)) _taskNames.Remove(kvp.Key);
                else if (task == taskFromReference) return kvp.Key;
            return null;
        }
    }
}