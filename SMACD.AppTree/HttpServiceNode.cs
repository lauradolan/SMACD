using SMACD.AppTree.Details;
using System.Linq;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents an HTTP service accessible via a specific protocol and port
    /// </summary>
    public class HttpServiceNode : ServiceNode, IAppTreeNode<HttpServiceDetails>
    {
        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "Compass.AppTree.HttpServiceNodeView";

        /// <summary>
        ///     Details around an HTTP Service
        /// </summary>
        public new Versionable<HttpServiceDetails> Detail { get; set; } = new Versionable<HttpServiceDetails>();

        /// <summary>
        ///     Get a child URL segment
        /// </summary>
        /// <param name="urlSegment">URL segment</param>
        /// <returns></returns>
        public UrlNode this[string urlSegment]
        {
            get
            {
                UrlNode result = ChildrenAre<UrlNode>(u => u.UrlSegment == urlSegment).FirstOrDefault();
                if (result == null)
                {
                    result = new UrlNode(this, urlSegment);
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Represents an HTTP service accessible via a specific protocol and port
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="identifiers">Identifiers for node</param>
        public HttpServiceNode(HostNode parent, params string[] identifiers) : base(parent, identifiers)
        {
        }

        /// <summary>
        ///     Represents an HTTP service accessible via a specific protocol and port
        /// </summary>
        public HttpServiceNode() { }

        /// <summary>
        ///     String representation of HTTP Service artifact
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"HTTP Service on port {Protocol}/{Port}";
        }
    }
}