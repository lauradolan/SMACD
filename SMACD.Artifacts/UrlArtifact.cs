using SMACD.Artifacts.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents a segment of a URL
    /// </summary>
    public class UrlArtifact : Artifact
    {
        /// <summary>
        ///     URL metadata
        /// </summary>
        public Versionable<UrlMetadata> Metadata { get; set; } = new Versionable<UrlMetadata>();

        /// <summary>
        ///     String representing this segment of a URL
        /// </summary>
        public string UrlSegment => NiceIdentifier;

        /// <summary>
        ///     Requests which can be made against this URL
        /// </summary>
        public IReadOnlyList<UrlRequestArtifact> Requests => Children.Where(c => c is UrlRequestArtifact).Cast<UrlRequestArtifact>().ToList().AsReadOnly();

        /// <summary>
        ///     Add a Request description to this URL segment
        /// </summary>
        /// <param name="method">HTTP Method</param>
        /// <param name="fields">Fields to send</param>
        /// <param name="headers">Headers to send</param>
        public UrlRequestArtifact AddRequest(string method, IDictionary<string, string> fields, IDictionary<string, string> headers)
        {
            var result = new UrlRequestArtifact() { Parent = this };
            result.Identifiers.Add($"{method.ToString().ToUpper()} ({HashCode.Combine(fields, headers)})");
            foreach (var kvp in fields) result.Fields.Add(kvp.Key, kvp.Value);
            foreach (var kvp in headers) result.Headers.Add(kvp.Key, kvp.Value);            
            result.BeginFiringEvents();
            Children.Add(result);
            return result;
        }

        /// <summary>
        ///     Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlArtifact this[string urlSegment]
        {
            get
            {
                UrlArtifact result = (UrlArtifact)Children.FirstOrDefault(d => d is UrlArtifact && d.Identifiers.Contains(urlSegment));
                if (result == null)
                {
                    result = new UrlArtifact { Parent = this };
                    result.Identifiers.Add(urlSegment);
                    result.BeginFiringEvents();
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Get entire URL from all segments (assuming this item is the last URL segment)
        /// </summary>
        /// <param name="url">Built URL</param>
        /// <returns></returns>
        public string GetUrl(string url = null)
        {
            url = "/" + UrlSegment + url;
            if (Parent is HttpServicePortArtifact)
            {
                return $"{((HttpServicePortArtifact)Parent).Host.Hostname}" +
                       ":" + $"{((HttpServicePortArtifact)Parent).Port}" +
                       url;
            }

            if (Parent is UrlArtifact)
            {
                return ((UrlArtifact)Parent).GetUrl(url);
            }

            throw new Exception("Invalid artifact tree");
        }

        /// <summary>
        ///     String representation of URL segment
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Children.Any())
            {
                return $"URL Segment '/{UrlSegment}/'";
            }

            return $"URL Segment '{UrlSegment}'";
        }
    }
}