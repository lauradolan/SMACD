﻿using Newtonsoft.Json;
using System;

namespace SMACD.SDK
{
    public abstract class ExtensionReport
    {
        /// <summary>
        /// Task descriptor generating the Extension instance
        /// </summary>
        public TaskDescriptor TaskDescriptor { get; set; }

        public TimeSpan Runtime { get; set; }

        public int RawPointsScored { get; set; }
        public int MaximumPointsAvailable { get; set; }
        public double AdjustedScore => MaximumPointsAvailable > 0 ? (double)RawPointsScored / MaximumPointsAvailable : 0;

        /// <summary>
        /// Create a blank report
        /// </summary>
        /// <returns></returns>
        public static ExtensionReport Blank()
        {
            return new BlankExtensionReport();
        }

        /// <summary>
        /// Create an error-containing report
        /// </summary>
        /// <param name="ex">Exception generated</param>
        /// <returns></returns>
        public static ExtensionReport Error(Exception ex)
        {
            return new ErroredExtensionReport(ex);
        }

        /// <summary>
        /// Generate a string representative of the report object's content
        /// </summary>
        /// <returns></returns>
        public abstract string GetReportContent();

        /// <summary>
        /// Finalize report by disconnecting TaskDescriptor from recursive loops
        /// </summary>
        /// <returns></returns>
        public ExtensionReport FinalizeReport()
        {
            TaskDescriptor.ArtifactRoot = null;
            ((QueuedTaskDescriptor)TaskDescriptor).ActionTask = null;
            ((QueuedTaskDescriptor)TaskDescriptor).Result = null;
            return this;
        }

        public virtual string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static ExtensionReport Deserialize(string serializedData)
        {
            return (ExtensionReport)JsonConvert.DeserializeObject(serializedData);
        }

        protected ExtensionReport() { }
        protected ExtensionReport(string serializedData)
        {
        }
    }

    public class BlankExtensionReport : ExtensionReport
    {
        public override string GetReportContent()
        {
            return "";
        }
    }

    public class ErroredExtensionReport : ExtensionReport
    {
        public Exception Exception { get; set; }
        public ErroredExtensionReport(Exception exception)
        {
            Exception = exception;
        }

        public override string GetReportContent()
        {
            return $"Error: {Exception}";
        }
    }
}
