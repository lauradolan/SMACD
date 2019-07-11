using System;
using System.Collections.Generic;
using System.Net.Http;
using SMACD.Shared.Attributes;
using YamlDotNet.Serialization;

namespace SMACD.Shared.Resources
{
    /// <summary>
    ///     Web Resource (HTTP)
    /// </summary>
    [ResourceIdentifier("http")]
    public class HttpResource : Resource
    {
        /// <summary>
        ///     URL for this endpoint
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        ///     URI object to analyze pieces of URL
        /// </summary>
        [YamlIgnore]
        public Uri UriInstance => new Uri(Url);

        /// <summary>
        ///     Method (GET/POST/etc) used to access this endpoint
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        ///     HttpMethod object to validate Method
        /// </summary>
        [YamlIgnore]
        public HttpMethod MethodInstance => new HttpMethod(Method);

        /// <summary>
        ///     Fields submitted to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     HTTP headers to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public static HttpResource GenerateGet(string url, IDictionary<string, string> headers = null)
        {
            return new HttpResource
            {
                Method = "GET", Url = url, Headers = headers != null ? headers : new Dictionary<string, string>(),
                SystemCreated = true
            };
        }

        public static HttpResource GeneratePost(string url, IDictionary<string, string> fields,
            IDictionary<string, string> headers = null)
        {
            return new HttpResource
            {
                Method = "POST", Url = url, Fields = fields,
                Headers = headers != null ? headers : new Dictionary<string, string>(), SystemCreated = true
            };
        }

        public bool MatchesExceptQuery(HttpResource that)
        {
            if (UriInstance.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) !=
                that.UriInstance.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped))
                return false;
            if (UriInstance.GetComponents(UriComponents.Path, UriFormat.Unescaped) !=
                that.UriInstance.GetComponents(UriComponents.Path, UriFormat.Unescaped))
                return false;
            if (!MethodInstance.Equals(that.MethodInstance))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HttpResource))
                return false;
            var that = (HttpResource) obj;

            if (!MatchesExceptQuery(that))
                return false;
            if (UriInstance.GetComponents(UriComponents.Query, UriFormat.Unescaped) !=
                that.UriInstance.GetComponents(UriComponents.Query, UriFormat.Unescaped))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                UriInstance.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
                UriInstance.GetComponents(UriComponents.Path, UriFormat.Unescaped),
                UriInstance.GetComponents(UriComponents.Query, UriFormat.Unescaped),
                MethodInstance
            );
        }

        public override string ToString()
        {
            var str = $"{Method} {UriInstance}";
            if (Fields.Count > 0)
                str += $" ({Fields.Count} fields)";
            if (Headers.Count > 0)
                str += $" ({Headers.Count} headers)";
            return str;
        }
    }
}