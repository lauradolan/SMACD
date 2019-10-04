using System;
using System.Linq;
using System.Net.Http;
using System.Web;
using SMACD.Artifacts;

namespace Synthesys.SDK
{
    public static class UrlHelper
    {
        public static UrlArtifact GeneratePathArtifacts(HttpServicePortArtifact httpService, string url, string method)
        {
            if (!url.StartsWith("http"))
                url = "http://" + httpService.Host.Hostname + ":" + httpService.Port + url;

            var uri = new Uri(url);

            var scheme = uri.Scheme;
            var host = uri.Host;
            var pieces = uri.AbsolutePath.Split('/').ToList();
            var queryParameters = HttpUtility.ParseQueryString(uri.Query);

            // Create path through tree based on path pieces
            var artifact = httpService["/"];
            foreach (var piece in pieces)
            {
                artifact = artifact[piece];
            }

            // Assign HTTP Verb to last piece before query parameters
            if (!string.IsNullOrEmpty(method))
            {
                if (method.ToUpper() == "GET")
                    artifact.Method = HttpMethod.Get;
                else if (method.ToUpper() == "POST")
                    artifact.Method = HttpMethod.Post;
                else if (method.ToUpper() == "PUT")
                    artifact.Method = HttpMethod.Put;
                else if (method.ToUpper() == "DELETE")
                    artifact.Method = HttpMethod.Delete;
                else if (method.ToUpper() == "HEAD")
                    artifact.Method = HttpMethod.Head;
                else if (method.ToUpper() == "TRACE") artifact.Method = HttpMethod.Trace;
            }

            foreach (var key in queryParameters.AllKeys)
            {
                artifact.Requests.Add(new UrlRequestArtifact()
                {
                    Fields = new ObservableDictionary<string, string>()
                    {
                        {
                            key, queryParameters[key]
                        }
                    }
                });
            }

            return artifact;
        }
    }
}