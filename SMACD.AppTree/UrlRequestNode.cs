using SMACD.AppTree.Details;
using System.Linq;
using System.Net.Http;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents a single request to a URL in some part of the application
    /// </summary>
    public class UrlRequestNode : AppTreeNode, IAppTreeNode<Details.UrlRequestDetails>
    {
        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "Compass.AppTree.UrlRequestNodeView";

        /// <summary>
        ///     Entire URL associated with this request
        /// </summary>
        public string Url => (Parent as UrlNode).GetEntireUrl();

        /// <summary>
        ///     HTTP method used to access URL
        /// </summary>
        public HttpMethod Method
        {
            get
            {
                if (string.IsNullOrEmpty(NiceIdentifier))
                {
                    return null;
                }

                string method = NiceIdentifier.Split(' ')[0];
                if (method.ToUpper() == "GET")
                {
                    return HttpMethod.Get;
                }
                else if (method.ToUpper() == "POST")
                {
                    return HttpMethod.Post;
                }
                else if (method.ToUpper() == "PUT")
                {
                    return HttpMethod.Put;
                }
                else if (method.ToUpper() == "DELETE")
                {
                    return HttpMethod.Delete;
                }
                else if (method.ToUpper() == "HEAD")
                {
                    return HttpMethod.Head;
                }
                else if (method.ToUpper() == "TRACE")
                {
                    return HttpMethod.Trace;
                }

                return null;
            }
        }

        /// <summary>
        ///     Fields to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Fields { get; set; } = new ObservableDictionary<string, string>();

        /// <summary>
        ///     Headers to be sent with request
        /// </summary>
        public ObservableDictionary<string, string> Headers { get; set; } = new ObservableDictionary<string, string>();

        /// <summary>
        ///     Details around a URL segment
        /// </summary>
        public Versionable<UrlRequestDetails> Detail { get; set; } = new Versionable<UrlRequestDetails>();

        /// <summary>
        ///     Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlNode this[string urlSegment]
        {
            get
            {
                UrlNode result = ChildrenAre<UrlNode>(n => n.UrlSegment == urlSegment).FirstOrDefault();
                if (result == null)
                {
                    result = new UrlNode { Parent = this };
                    result.Identifiers.Add(urlSegment);
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Represents a single request to a URL in some part of the application
        /// </summary>
        public UrlRequestNode()
        {
            Fields.CollectionChanged += (s, e) => { if (Root != null) NotifyChanged(); };
            Headers.CollectionChanged += (s, e) => { if (Root != null) NotifyChanged(); };
        }

        /// <summary>
        ///     Get entire URL from all segments (assuming this item is the last URL segment)
        /// </summary>
        /// <returns></returns>
        public string GetEntireUrl()
        {
            System.Collections.Generic.List<AppTreeNode> path = GetPath();
            HttpServiceNode service = path.FirstOrDefault(p => p is HttpServiceNode) as HttpServiceNode;
            string url = string.Join('/', path.Where(p => p is UrlNode).Select(n => ((UrlNode)n).UrlSegment));
            return $"{service.Host.Hostname}:{service.Port}/{url}";
        }

        /// <summary>
        ///     String representation of URL segment
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Method} Request Against '{Url}'";
        }
    }
}