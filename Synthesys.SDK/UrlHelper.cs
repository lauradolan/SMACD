using System;
using System.Linq;
using System.Web;
using SMACD.Artifacts;

namespace Synthesys.SDK
{
    /// <summary>
    ///     Functions to work with URLs in the Artifact correlation tree
    /// </summary>
    public static class UrlHelper
    {
        /// <summary>
        ///     Generate artifacts under the given Artifact for each segment of a given URL and request method
        /// </summary>
        /// <param name="httpService">Base artifact</param>
        /// <param name="url">URL to create data from</param>
        /// <param name="method">HTTP request method</param>
        /// <returns>Artifact representing leaf of URL</returns>
        public static UrlArtifact GeneratePathArtifacts(HttpServicePortArtifact httpService, string url, string method)
        {
            if (!url.StartsWith("http"))
                url = "http://" + httpService.Host.Hostname + ":" + httpService.Port + url;

            var uri = new Uri(url);

            var scheme = uri.Scheme;
            var host = uri.Host;
            var pieces = uri.AbsolutePath.Split('/').Where(e => !string.IsNullOrEmpty(e)).ToList();
            var queryParameters = HttpUtility.ParseQueryString(uri.Query);

            // Create path through tree based on path pieces
            var artifact = httpService["/"];
            foreach (var piece in pieces)
            {
                artifact = artifact[piece];
            }

            // "artifact" is leaf URL, create request here
            var request = new UrlRequestArtifact();

            foreach (var key in queryParameters.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
                request.Fields[key] = queryParameters[key];

            request.Identifiers.Add($"{method.ToUpper()} ({HashCode.Combine(request.Fields, request.Headers)})");
            artifact.Requests.Add(request);

            return artifact;
        }
    }
}