using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;

namespace SMACD.Artifacts
{
    public class UrlArtifact : Artifact
    {
        /// <summary>
        /// Artifact Identifier
        /// </summary>
        public override string Identifier => $"{(Method == null ? "" : $"{Method} ")}{UrlSegment}";

        private HttpMethod method = null;
        private string urlSegment;

        /// <summary>
        /// HTTP method used to access URL
        /// </summary>
        public HttpMethod Method
        {
            get => method;
            set
            {
                method = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// URL Segment (file/directory)
        /// </summary>
        public string UrlSegment
        {
            get => urlSegment;
            set
            {
                urlSegment = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Requests which can be made against this URL
        /// </summary>
        public ObservableCollection<UrlRequestArtifact> Requests { get; set; } = new ObservableCollection<UrlRequestArtifact>();

        public UrlArtifact()
        {
            Requests.CollectionChanged += (s, e) => NotifyChanged();
        }

        public override void Disconnect()
        {
            if (Requests != null)
                foreach (var req in Requests)
                    req.Disconnect();
            base.Disconnect();
        }

        public override void Connect(Artifact parent = null)
        {
            if (Requests != null)
                foreach (var req in Requests)
                    req.Connect(this);
            base.Connect(parent);
        }

        /// <summary>
        /// Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlArtifact this[string urlSegment]
        {
            get
            {
                UrlArtifact result = (UrlArtifact)Children.FirstOrDefault(d => ((UrlArtifact)d).Identifier == urlSegment || ((UrlArtifact)d).UrlSegment == urlSegment);
                if (result == null)
                {
                    result = new UrlArtifact()
                    {
                        Parent = this,
                        UrlSegment = urlSegment,
                        Method = HttpMethod.Get
                    };
                    result.BeginFiringEvents();
                    Children.Add(result);
                }
                return result;
            }
        }

        /// <summary>
        /// Get entire URL from all segments (assuming this item is the last URL segment)
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
            else if (Parent is UrlArtifact)
            {
                return ((UrlArtifact)Parent).GetUrl(url);
            }
            else
            {
                throw new Exception("Invalid artifact tree");
            }
        }

        public override string ToString()
        {
            return $"URL Segment '{UrlSegment}'";
        }
    }
}
