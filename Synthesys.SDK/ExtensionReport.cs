using System;
using Newtonsoft.Json;

namespace Synthesys.SDK
{
    public class ExtensionReport
    {
        protected ExtensionReport()
        {
        }

        protected ExtensionReport(string serializedData)
        {
        }

        /// <summary>
        ///     Task descriptor generating the Extension instance
        /// </summary>
        public TaskDescriptor TaskDescriptor { get; set; }

        /// <summary>
        ///     How long the Extension took to execute
        /// </summary>
        public TimeSpan Runtime { get; set; }

        /// <summary>
        ///     Number of points scored on the Extension's own scale
        /// </summary>
        public int RawPointsScored { get; set; }

        /// <summary>
        ///     Maximum number of points available on the Extension's own scale
        /// </summary>
        public int MaximumPointsAvailable { get; set; }

        /// <summary>
        ///     Name of the View describing this ExtensionReport
        /// </summary>
        public virtual string ReportViewName => null;

        /// <summary>
        ///     Name of the summary control to represent this ExtensionReport
        /// </summary>
        public virtual string ReportSummaryName => null;

        /// <summary>
        ///     Adjusted score out of 1.0
        /// </summary>
        public double AdjustedScore =>
            MaximumPointsAvailable > 0 ? (double) RawPointsScored / MaximumPointsAvailable : 0;

        /// <summary>
        ///     Create a blank report
        /// </summary>
        /// <returns></returns>
        public static ExtensionReport Blank()
        {
            return new BlankExtensionReport();
        }

        /// <summary>
        ///     Create an error-containing report
        /// </summary>
        /// <param name="ex">Exception generated</param>
        /// <returns></returns>
        public static ExtensionReport Error(Exception ex)
        {
            return new ErroredExtensionReport(ex);
        }

        /// <summary>
        ///     Generate a string representative of the report object's content
        /// </summary>
        /// <returns></returns>
        public virtual string GetReportContent() => "Report is Empty";

        /// <summary>
        ///     Finalize report by disconnecting TaskDescriptor from recursive loops
        /// </summary>
        /// <returns></returns>
        public ExtensionReport FinalizeReport()
        {
            TaskDescriptor.ArtifactRoot = null;
            ((QueuedTaskDescriptor) TaskDescriptor).ActionTask = null;
            ((QueuedTaskDescriptor) TaskDescriptor).Result = null;
            return this;
        }

        public virtual string Serialize()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
            });
        }

        protected virtual ExtensionReport DeserializeFromString(string serializedData)
        {
            return (ExtensionReport)JsonConvert.DeserializeObject(serializedData, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
            });
        }

        public static ExtensionReport Deserialize(string serializedData) => new ExtensionReport().DeserializeFromString(serializedData);
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
        public ErroredExtensionReport(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; set; }

        public override string GetReportContent()
        {
            return $"Error: {Exception}";
        }
    }
}