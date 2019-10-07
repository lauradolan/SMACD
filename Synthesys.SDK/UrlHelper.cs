using SMACD.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
            {
                url = "http://" + httpService.Host.Hostname + ":" + httpService.Port + url;
            }

            Uri uri = new Uri(url);

            string scheme = uri.Scheme;
            string host = uri.Host;
            System.Collections.Generic.List<string> pieces = uri.AbsolutePath.Split('/').Where(e => !string.IsNullOrEmpty(e)).ToList();
            System.Collections.Specialized.NameValueCollection queryParameters = HttpUtility.ParseQueryString(uri.Query);

            // Create path through tree based on path pieces
            UrlArtifact artifact = httpService["/"];
            foreach (string piece in pieces)
            {
                artifact = artifact[piece];
            }

            // "artifact" is leaf URL, create request here
            UrlRequestArtifact request = new UrlRequestArtifact();

            foreach (string key in queryParameters.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
            {
                request.Fields[key] = queryParameters[key];
            }

            artifact.AddRequest(method, request.Fields, new Dictionary<string, string>());

            return artifact;
        }
    }
}