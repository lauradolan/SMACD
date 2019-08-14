using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;

namespace SMACD.Artifacts
{
    public class UrlArtifact : Artifact
    {
        public override string Identifier => $"{(Method == null ? "" : $"{Method} ")}{UrlSegment}";

        private HttpMethod method = null;
        private string urlSegment;

        public HttpMethod Method
        {
            get => method;
            set
            {
                method = value;
                NotifyChanged();
            }
        }

        public string UrlSegment
        {
            get => urlSegment;
            set
            {
                urlSegment = value;
                NotifyChanged();
            }
        }

        public ObservableCollection<UrlRequestArtifact> Requests { get; set; } = new ObservableCollection<UrlRequestArtifact>();

        public UrlArtifact()
        {
            Requests.CollectionChanged += (s, e) => NotifyChanged();
        }

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
