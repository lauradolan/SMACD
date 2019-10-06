using System.Linq;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents an HTTP service accessible via a specific protocol and port
    /// </summary>
    public class HttpServicePortArtifact : ServicePortArtifact
    {
        /// <summary>
        ///     Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlArtifact this[string urlSegment]
        {
            get
            {
                var result = (UrlArtifact)Children.FirstOrDefault(d => d.Identifiers.Contains(urlSegment));
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
        ///     String representation of HTTP Service artifact
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"HTTP Service on port {Protocol}/{Port}";
        }
    }
}