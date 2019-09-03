using SMACD.Artifacts;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SMACD.SDK
{
    public static class UrlHelper
    {
        public static UrlArtifact GeneratePathArtifacts(HttpServicePortArtifact httpService, string url, string method)
        {
            List<string> pieces = url.Split('/').ToList();
            pieces.RemoveAll(p => string.IsNullOrEmpty(p));
            UrlArtifact artifact = httpService["/"];
            foreach (string piece in pieces)
            {
                if (pieces.Last() == piece)
                {
                    if (method.ToUpper() == "GET")
                    {
                        artifact[piece].Method = HttpMethod.Get;
                    }
                    else if (method.ToUpper() == "POST")
                    {
                        artifact[piece].Method = HttpMethod.Post;
                    }
                    else if (method.ToUpper() == "PUT")
                    {
                        artifact[piece].Method = HttpMethod.Put;
                    }
                    else if (method.ToUpper() == "DELETE")
                    {
                        artifact[piece].Method = HttpMethod.Delete;
                    }
                    else if (method.ToUpper() == "HEAD")
                    {
                        artifact[piece].Method = HttpMethod.Head;
                    }
                    else if (method.ToUpper() == "TRACE")
                    {
                        artifact[piece].Method = HttpMethod.Trace;
                    }
                }
                artifact = artifact[piece];
            }
            return artifact;
        }
    }
}
