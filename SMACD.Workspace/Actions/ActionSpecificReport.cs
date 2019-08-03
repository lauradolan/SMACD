using SMACD.Workspace.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Represents the base of a Report that describes the outcome of an Action
    /// </summary>
    public class ActionSpecificReport
    {
        /// <summary>
        /// Task that generated this Result (Action+config)
        /// </summary>
        public ResultProvidingTaskDescriptor GeneratingTask { get; internal set; }

        /// <summary>
        /// Amount of time taken to execute the Action
        /// </summary>
        public TimeSpan Runtime { get; internal set; }

        /// <summary>
        /// Emit a new Blank Action-Specific Report
        /// </summary>
        public static ActionSpecificReport Blank => new ActionSpecificReport();
    }
}
