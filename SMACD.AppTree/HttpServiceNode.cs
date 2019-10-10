using System.Linq;
using SMACD.AppTree.Details;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents an HTTP service accessible via a specific protocol and port
    /// </summary>
    public class HttpServiceNode : ServiceNode, IAppTreeNode<Details.HttpServiceDetails>
    {
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
                    result = new UrlNode { Parent = this };
                    result.Identifiers.Add(urlSegment);
                    Children.Add(result);
                }

                return result;
            }
        }

        /// <summary>
        ///     Details around an HTTP Service
        /// </summary>
        public Versionable<HttpServiceDetails> Detail { get; set; } = new Versionable<HttpServiceDetails>();

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