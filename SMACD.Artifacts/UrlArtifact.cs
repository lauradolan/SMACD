using SMACD.Artifacts.Metadata;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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
        public string UrlSegment => Identifiers.FirstOrDefault();

        /// <summary>
        ///     Requests which can be made against this URL
        /// </summary>
        public ObservableCollection<UrlRequestArtifact> Requests { get; set; } = new ObservableCollection<UrlRequestArtifact>();

        /// <summary>
        ///     Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlArtifact this[string urlSegment]
        {
            get
            {
                UrlArtifact result = (UrlArtifact)Children.FirstOrDefault(d => d.Identifiers.Contains(urlSegment));
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
        ///     Represents a segment of a URL
        /// </summary>
        public UrlArtifact()
        {
            Requests.CollectionChanged += (s, e) => NotifyChanged();
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