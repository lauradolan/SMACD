using System.Linq;
using System.Net.Http;

namespace SMACD.Artifacts
{
    public class HttpServicePortArtifact : ServicePortArtifact
    {
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

        public override string ToString()
        {
            return $"HTTP Service on port {Protocol}/{Port} ({ServiceName})";
        }
    }
}
