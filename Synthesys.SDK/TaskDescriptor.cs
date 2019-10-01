﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SMACD.Artifacts;

namespace Synthesys.SDK
{
    /// <summary>
    ///     Task descriptor used to submit a Task to be run
    /// </summary>
    public class TaskDescriptor
    {
        /// <summary>
        ///     Action identifier
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        ///     Options to be configured on Action
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Root Artifact under which data will be placed
        /// </summary>
        public Artifact ArtifactRoot { get; set; }

        /// <summary>
        ///     Pointer to business elements (project elements)
        /// </summary>
        public ProjectPointer ProjectPointer { get; set; }
    }

    /// <summary>
    ///     Task descriptor that includes a Result from a Task
    /// </summary>
    public class ResultProvidingTaskDescriptor : TaskDescriptor
    {
        /// <summary>
        ///     Reports generated by Action and triggered Reactions
        /// </summary>
        public List<ExtensionReport> Results { get; set; }
    }

    public class QueuedTaskDescriptor : ResultProvidingTaskDescriptor
    {
        public Task<List<ExtensionReport>> ActionTask { get; set; }
    }
}