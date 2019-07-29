using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SMACD.PluginHost.Resources
{
    /// <summary>
    ///     Web Resource (HTTP)
    /// </summary>
    public class HttpResource : Resource
    {
        /// <summary>
        ///     URL for this endpoint
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        ///     URI object to analyze pieces of URL
        /// </summary>
        public Uri UriInstance => new Uri(Url);

        /// <summary>
        ///     Method (GET/POST/etc) used to access this endpoint
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        ///     HttpMethod object to validate Method
        /// </summary>
        public HttpMethod MethodInstance => new HttpMethod(Method);

        /// <summary>
        ///     Fields submitted to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     HTTP headers to be used in this endpoint
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public override string GetDescription()
        {
            var str = $"{UriInstance.Scheme} {Method} {Url}";
            if (Fields != null && Fields.Count > 0)
                str += $" (Fields: {Fields.Count})";
            if (Headers != null && Headers.Count > 0)
                str += $" (Headers: {Headers.Count})";
            return str;
        }

        public override bool IsApproximateTo(Resource resource)
        {
            var that = resource as HttpResource;
            if (that == null) return false;
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

            if (!IsApproximateTo(that))
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
            return GetDescription();
        }
    }
}