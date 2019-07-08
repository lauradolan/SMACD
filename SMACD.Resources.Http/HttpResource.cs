using SMACD.Shared;
using SMACD.Shared.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.Resources.Http
{
    /// <summary>
    /// Web Resource (HTTP)
    /// </summary>
    [ResourceIdentifier("http")]
    public class HttpResource : IResource
    {
        /// <summary>
        /// Resource Identifier
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// URL for this endpoint
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Method (GET/POST/etc) used to access this endpoint
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Fields submitted to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// HTTP headers to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>  
        /// If the Resource was created by the system
        /// </summary>
        public bool SystemCreated { get; set; }

        public override string ToString()
        {
            var fieldStr = string.Join("\n", Fields.Select(f => $"   {f.Key.PadRight(20)} => {f.Value}"));
            var headerStr = string.Join("\n", Headers.Select(h => $"   {h.Key.PadRight(20)} => {h.Value}"));
            return $"{ResourceId} (HTTP{(this is HttpsResource ? "S" : "")}) " + "\n" +
                $"Target: {Method} {Url}" + " \n" +
                $"Fields: " + "\n" + fieldStr + " \n" +
                $"Headers: " + "\n" + headerStr + " \n";
        }

        public IResource GetWithTransformations(IDictionary<string, string> transformations)
        {
            // TODO: Implement this so that a resource can always have a field, always have a header, etc
            return this;
        }
    }
}
