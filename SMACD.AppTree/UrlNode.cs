using SMACD.AppTree.Details;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents a single URL segment (directory or file) in some part of the application
    /// </summary>
    public class UrlNode : AppTreeNode, IAppTreeNode<UrlDetails>
    {
        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "Compass.AppTree.UrlNodeView";

        /// <summary>
        ///     String representing this segment of a URL
        /// </summary>
        public string UrlSegment => NiceIdentifier;

        /// <summary>
        ///     If this URL segment has any Requests
        /// </summary>
        public bool HasRequests => Children.Any(c => c is UrlRequestNode);

        /// <summary>
        ///     Requests to this URL
        /// </summary>
        public IReadOnlyCollection<UrlRequestNode> Requests => ChildrenAre<UrlRequestNode>();

        /// <summary>
        ///     Details around a URL segment
        /// </summary>
        public Versionable<UrlDetails> Detail { get; set; } = new Versionable<UrlDetails>();

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
        ///     Add a Request description to this URL segment
        /// </summary>
        /// <param name="method">HTTP Method</param>
        /// <param name="fields">Fields to send</param>
        /// <param name="headers">Headers to send</param>
        public UrlRequestNode AddRequest(string method, IDictionary<string, string> fields, IDictionary<string, string> headers)
        {
            UrlRequestNode result = new UrlRequestNode() { Parent = this };
            result.Identifiers.Add($"{method.ToString().ToUpper()} ({string.Join(", ", fields.Keys)}) ({string.Join(", ", fields.Select(f => $"{f.Key} => {f.Value}"))})");
            foreach (KeyValuePair<string, string> kvp in fields)
            {
                result.Fields.Add(kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<string, string> kvp in headers)
            {
                result.Headers.Add(kvp.Key, kvp.Value);
            }

            if (Children.Any(c => c.NiceIdentifier == result.NiceIdentifier))
                return Children.FirstOrDefault(c => c.NiceIdentifier == result.NiceIdentifier) as UrlRequestNode;

            Children.Add(result);
            return result;
        }

        /// <summary>
        ///     Get entire URL from all segments (assuming this item is the last URL segment)
        /// </summary>
        /// <returns></returns>
        public string GetEntireUrl()
        {
            List<AppTreeNode> path = GetPath();
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
            if (Children.Any() && UrlSegment != "/")
            {
                return $"URL Segment '/{UrlSegment}/'";
            }

            return $"URL Segment '{UrlSegment}'";
        }
    }
}