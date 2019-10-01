using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using SMACD.Artifacts;

namespace Synthesys.SDK
{
    public static class UrlHelper
    {
        public static UrlArtifact GeneratePathArtifacts(HttpServicePortArtifact httpService, string url, string method)
        {
            var pieces = url.Split('/').ToList();
            pieces.RemoveAll(p => string.IsNullOrEmpty(p));
            var artifact = httpService["/"];

            var queryParameters = new Dictionary<string, string>();
            if (pieces.Count > 0 && pieces.Last().Contains('?'))
            {
                var actualPiece = pieces.Last().Split('?')[0];
                var query = pieces.Last().Split('?')[1];
                var queryItems = query.Split('&').ToList();
                queryItems.ForEach(q => queryParameters.Add(q.Split('=')[0], q.Split('=')[1]));

                pieces.RemoveAt(pieces.Count - 1);
                pieces.Add(actualPiece);
            }

            foreach (var piece in pieces)
            {
                if (pieces.Last() == piece)
                {
                    if (method.ToUpper() == "GET")
                        artifact[piece].Method = HttpMethod.Get;
                    else if (method.ToUpper() == "POST")
                        artifact[piece].Method = HttpMethod.Post;
                    else if (method.ToUpper() == "PUT")
                        artifact[piece].Method = HttpMethod.Put;
                    else if (method.ToUpper() == "DELETE")
                        artifact[piece].Method = HttpMethod.Delete;
                    else if (method.ToUpper() == "HEAD")
                        artifact[piece].Method = HttpMethod.Head;
                    else if (method.ToUpper() == "TRACE") artifact[piece].Method = HttpMethod.Trace;
                }

                artifact = artifact[piece];
            }

            return artifact;
        }
    }
}