using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using System;
using System.Collections.Generic;

namespace SMACD.SDK
{
    public abstract class ExtensionReport
    {
        public TaskDescriptor TaskDescriptor { get; set; }
        public string ExtensionIdentifier { get; set; }
        public Dictionary<string, string> Options { get; set; }
        public Artifact ArtifactRoot { get; set; }

        public TimeSpan Runtime { get; set; }

        public static ExtensionReport Blank(
            string extensionIdentifier,
            Artifact node = null,
            Dictionary<string, string> options = null)
        {
            return new BlankExtensionReport(extensionIdentifier, node, options);
        }

        public static ExtensionReport Error(
           string extensionIdentifier,
           Exception ex,
           Artifact node = null,
           Dictionary<string, string> options = null)
        {
            return new ErroredExtensionReport(extensionIdentifier, node, ex, options);
        }

        public ExtensionReport(string extensionIdentifier,
            Artifact artifactRoot,
            Dictionary<string, string> options = null)
        {
            if (options == null)
            {
                options = new Dictionary<string, string>();
            }

            ExtensionIdentifier = extensionIdentifier;
            Options = options;
            ArtifactRoot = artifactRoot;
        }

        public abstract string GetReportContent();

        public ExtensionReport FinalizeReport()
        {
            ArtifactRoot = null;
            TaskDescriptor.ArtifactRoot = null;
            ((QueuedTaskDescriptor)TaskDescriptor).ActionTask = null;
            ((QueuedTaskDescriptor)TaskDescriptor).Result = null;
            return this;
        }
    }

    public class BlankExtensionReport : ExtensionReport
    {
        public BlankExtensionReport(string extensionIdentifier,
            Artifact artifactRoot,
            Dictionary<string, string> options = null) : base(extensionIdentifier, artifactRoot, options)
        { }

        public override string GetReportContent()
        {
            return "";
        }
    }

    public class ErroredExtensionReport : ExtensionReport
    {
        public Exception Exception { get; set; }

        public ErroredExtensionReport(string extensionIdentifier,
            Artifact artifactRoot,
            Exception exception,
            Dictionary<string, string> options = null) : base(extensionIdentifier, artifactRoot, options)
        {
            Exception = exception;
        }

        public override string GetReportContent()
        {
            return $"Error: {Exception}";
        }
    }
}
