using System.Collections.Generic;

namespace SMACD.AppTree
{
    /// <summary>
    ///     A single Generation of Paths which matched the requested address
    /// </summary>
    public class PathGeneration
    {
        /// <summary>
        ///     Path being tested
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     Index of segment within Path which matched for this Generation
        /// </summary>
        public int PathSegmentIndex { get; set; }

        /// <summary>
        ///     Segment which matched for this Generation
        /// </summary>
        public string PathSegment => Path.Split(PathParserExtensions.PATH_SEPARATOR)[PathSegmentIndex];

        /// <summary>
        ///     AppTreeNode linked to Generation
        /// </summary>
        public AppTreeNode MatchingNode { get; set; }

        /// <summary>
        ///     Nodes which matched in the next Generation
        /// </summary>
        public List<PathGeneration> Children { get; set; } = new List<PathGeneration>();

        /// <summary>
        ///     If the node is matching the Path
        /// </summary>
        public bool IsResultNode { get; internal set; }

        /// <summary>
        ///     Retrieve all the nodes within this Path Generation set which are matched by the requested Path
        /// </summary>
        /// <returns></returns>
        public List<AppTreeNode> GetResultNodes()
        {
            var list = new List<AppTreeNode>();
            GetResultNodes(ref list);
            return list;
        }
        private void GetResultNodes(ref List<AppTreeNode> nodes)
        {
            if (IsResultNode)
                nodes.Add(MatchingNode);
            foreach (var child in Children) child.GetResultNodes(ref nodes);
        }
    }
}
