using SMACD.Artifacts.Metadata;
using System.Linq;
using System.Net.Http;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents a request to a URL
    /// </summary>
    public class UrlRequestArtifact : Artifact
    {
        /// <summary>
        ///     URL request metadata
        /// </summary>
        public Versionable<UrlRequestMetadata> Metadata { get; set; } = new Versionable<UrlRequestMetadata>();

        /// <summary>
        ///     HTTP method used to access URL
        /// </summary>
        public HttpMethod Method
        {
            get
            {
                var method = Identifiers.First();
                if (method.ToUpper() == "GET")
                    return HttpMethod.Get;
                else if (method.ToUpper() == "POST")
                    return HttpMethod.Post;
                else if (method.ToUpper() == "PUT")
                    return HttpMethod.Put;
                else if (method.ToUpper() == "DELETE")
                    return HttpMethod.Delete;
                else if (method.ToUpper() == "HEAD")
                    return HttpMethod.Head;
                else if (method.ToUpper() == "TRACE") return HttpMethod.Trace;
                return null;
            }
        }

        /// <summary>
        ///     Fields to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Fields { get; set; } = new ObservableDictionary<string, string>();

        /// <summary>
        ///     Headers to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Headers { get; set; } = new ObservableDictionary<string, string>();

        /// <summary>
        ///     Represents a request to a URL
        /// </summary>
        public UrlRequestArtifact()
        {
            Fields.CollectionChanged += (s, e) => NotifyChanged();
            Headers.CollectionChanged += (s, e) => NotifyChanged();
        }

        /// <summary>
        ///     String representation of URL Request
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "URL Request";
        }
    }
}