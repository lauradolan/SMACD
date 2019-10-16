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
                    result = new UrlNode(this);
                    result.Identifiers.Add(urlSegment);
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Represents a single request to a URL in some part of the application
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="identifiers">Identifiers for node</param>
        public UrlRequestNode(UrlNode parent, params string[] identifiers) : base(parent, identifiers)
        {
            Fields.CollectionChanged += (s, e) => { if (Root != null) NotifyChanged(); };
            Headers.CollectionChanged += (s, e) => { if (Root != null) NotifyChanged(); };
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
            var url = ((UrlNode)Parent).GetEntireUrl();
            url += string.Join("&", Fields.Select(field => $"{field.Key}={field.Value}"));
            return url;
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