using SMACD.AppTree.Evidence;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents a single node in a tree, including all navigation properties
    /// </summary>
    public class AppTreeNode
    {
        private const string PATH_SEPARATOR = "//";
        private const string SINGLE_GENERATION_WILDCARD = "*";
        private const string N_GENERATION_WILDCARD = "**";
        private static string[] CONTROL_CHARS = new string[] { SINGLE_GENERATION_WILDCARD, N_GENERATION_WILDCARD };

        /// <summary>
        ///     Root Artifact
        /// </summary>
        public RootNode Root => GetPath()?.FirstOrDefault() as RootNode;

        /// <summary>
        ///     Parent Artifact
        /// </summary>
        public AppTreeNode Parent { get; set; }

        /// <summary>
        ///     Children of Artifact
        /// </summary>
        public ObservableCollection<AppTreeNode> Children { get; set; } = new ObservableCollection<AppTreeNode>();

        /// <summary>
        ///     Get only Artifact Children of a specific type
        /// </summary>
        /// <typeparam name="TChild">Artifact child type</typeparam>
        /// <returns></returns>
        public IReadOnlyCollection<TChild> ChildrenAre<TChild>() => Children.Where(c => c is TChild).Cast<TChild>().ToList().AsReadOnly();

        /// <summary>
        ///     Get predicate-matching Artifact Children of a specific type
        /// </summary>
        /// <typeparam name="TChild">Artifact child type</typeparam>
        /// <returns></returns>
        public IReadOnlyCollection<TChild> ChildrenAre<TChild>(Predicate<TChild> predicate) => Children
            .Where(c => c is TChild)
            .Cast<TChild>()
            .Where(c => predicate(c)).ToList().AsReadOnly();

        /// <summary>
        ///     Artifact Identifier for path
        /// </summary>
        public List<string> Identifiers { get; } = new List<string>();

        /// <summary>
        ///     Get nice-name identifier for Artifact (first non-UUID)
        /// </summary>
        public string NiceIdentifier => Identifiers.FirstOrDefault(i => !Guid.TryParse(i, out Guid dummy));

        /// <summary>
        ///     Unique identifier
        /// </summary>
        public Guid UUID { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Data attachments providing evidence of correlating data pertaining to Artifact
        /// </summary>
        public EvidenceCollection Evidence { get; set; } = new EvidenceCollection();

        /// <summary>
        ///     Vulnerabilities related to Artifact
        /// </summary>
        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public virtual string NodeViewName { get; }

        /// <summary>
        ///     Get a child Artifact by its identifier
        /// </summary>
        /// <param name="uuid">Artifact UUID</param>
        /// <returns></returns>
        public AppTreeNode this[Guid uuid] => Children.FirstOrDefault(c => c.UUID == uuid);

        /// <summary>
        ///     Returns the leaf Artifact associated with a given UUID path
        /// </summary>
        /// <param name="path">UUID Path relative to this node</param>
        /// <returns></returns>
        public AppTreeNode GetNodeByRelativeUUIDPath(string path)
        {
            List<string> pathElements = path.Split(PATH_SEPARATOR).Where(p => p != null).ToList();
            if (pathElements.First() == UUID.ToString())
            {
                pathElements.RemoveAt(0);
            }

            AppTreeNode node = this;
            foreach (string element in pathElements)
            {
                Guid uuid = Guid.Parse(element);
                node = node.Children.FirstOrDefault(c => c.UUID == uuid);
                if (node == null)
                {
                    throw new Exception($"Path not valid. Element '{element}' failed");
                }
            }
            return node;
        }

        /// <summary>
        ///     Get a string representing an element from each node heading toward the root node
        /// </summary>
        /// <param name="selector">Selector for element's path string</param>
        /// <returns></returns>
        public string GetPath(Func<AppTreeNode, string> selector) => string.Join(PATH_SEPARATOR, GetPath().Select(n => selector(n)));

        /// <summary>
        ///     Get the string representing each element from here to the root, using their identifier text
        /// </summary>
        /// <returns>String representing each element from here to the root, using their identifier text</returns>
        public string GetDisplayPath() => GetPath(n => n.NiceIdentifier);

        /// <summary>
        ///     Get the string representing each element from here to the root, using their UUID
        /// </summary>
        /// <returns></returns>
        public string GetUUIDPath() => GetPath(n => n.UUID.ToString());

        /// <summary>
        ///     Get a list of nodes between this node and the root node, inclusive
        /// </summary>
        /// <returns></returns>
        public List<AppTreeNode> GetPath()
        {
            List<AppTreeNode> nodes = new List<AppTreeNode>();
            AppTreeNode node = this;
            while (node != null)
            {
                nodes.Add(node);
                node = node.Parent;
            }
            nodes.Reverse();
            return nodes;
        }

        /// <summary>
        ///     If the Artifact can be described by the given string path (may contain wildcards, etc)
        /// </summary>
        /// <param name="path">Path to test</param>
        /// <returns><c>TRUE</c> if the path describes the Artifact, otherwise <c>FALSE</c></returns>
        public bool IsDescribedByPath(string path)
        {
            var pathSegments = path.Split(PATH_SEPARATOR);
            var artifactPathToRoot = GetPath();
            var segmentIndex = 0;
            bool insideMultiGenerationalWildcard = false;

            foreach (var element in artifactPathToRoot)
            {
                // Bounds-check path segment
                if (segmentIndex > pathSegments.Length - 1)
                {
                    if (pathSegments.Last() == N_GENERATION_WILDCARD) return true;
                    return false;
                }
                var thisPathSegment = pathSegments[segmentIndex];

                // True/False if type specified, otherwise null
                var typeConstraintResult = MeetsTypeConstraints(element, thisPathSegment, out thisPathSegment);
                // True/False if non-wildcard specified, otherwise null
                var nameConstraintResult = MeetsNamingConstraints(element, thisPathSegment);
                var specificConstraintsMet = (typeConstraintResult.HasValue && typeConstraintResult.Value == true) ||
                                             (nameConstraintResult.HasValue && nameConstraintResult.Value == true);

                // If specific constraints were met for this node/segment, any long wildcards end
                if (specificConstraintsMet)
                    insideMultiGenerationalWildcard = false;

                // Didn't meet specific constraints but still inside wildcard
                if (insideMultiGenerationalWildcard)
                    continue;

                if (thisPathSegment == SINGLE_GENERATION_WILDCARD && typeConstraintResult.GetValueOrDefault(true))
                {
                    segmentIndex++;
                    continue;
                }
                else if (thisPathSegment == N_GENERATION_WILDCARD && typeConstraintResult.GetValueOrDefault(true))
                {
                    insideMultiGenerationalWildcard = true;
                    segmentIndex++;
                    continue;
                }
                else if (typeConstraintResult.GetValueOrDefault(true) && nameConstraintResult.GetValueOrDefault(true))
                {
                    segmentIndex++;
                    continue;
                }
            }
            if (segmentIndex > pathSegments.Length - 1)
                return true;
            return false;
        }

        private static bool? MeetsTypeConstraints(AppTreeNode element, string thisPathSegment, out string newTestString)
        {
            var matchTypeEnforcement = Regex.Match(thisPathSegment, "\\{(?<type>\\S+)\\}(?<name>\\S+)");
            if (matchTypeEnforcement.Success)
            {
                // Rewrite segment without Type enforcement
                newTestString = matchTypeEnforcement.Groups["name"].Value;

                // todo: cache-back type lookups, otherwise this will become VERY expensive
                var expectedTypeName = matchTypeEnforcement.Groups["type"].Value;
                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.DefinedTypes.Any(t => t.Name == expectedTypeName || t.FullName == expectedTypeName));
                if (asm == null)
                    throw new Exception($"Trigger Path required type safety check against expected type {expectedTypeName}, which does not exist");
                var expectedType = asm.DefinedTypes.First(t => t.Name == expectedTypeName || t.FullName == expectedTypeName).UnderlyingSystemType;

                return expectedType.IsAssignableFrom(element.GetType());
            }
            newTestString = thisPathSegment;
            return null;
        }

        private static bool? MeetsNamingConstraints(AppTreeNode element, string thisPathSegment)
        {
            if (!CONTROL_CHARS.Contains(thisPathSegment))
            {
                // Convert to Regex to support ?/* (1-char/n-char) wildcards
                var regexTest = "^" + Regex.Escape(thisPathSegment).Replace("\\?", ".").Replace("\\*", ".*") + "$";
                if (element.Identifiers.Any(i => Regex.IsMatch(i, regexTest)))
                    return true;
                return false;
            }
            return null;
        }

        /// <summary>
        ///     Notify root element to fire an ArtifactChanged event
        /// </summary>
        protected void NotifyChanged() => Root.InvokeTreeNodeChanged(this);

        /// <summary>
        ///     Notify root element to fire an ArtifactChildAdded event
        /// </summary>
        /// <param name="newChild">Child added</param>
        protected void NotifyChildAdded(AppTreeNode newChild) => Root.InvokeTreeChildAdded(newChild);

        /// <summary>
        ///     Notify root element to fire an ArtifactCreated event
        /// </summary>
        protected void NotifyCreated() => Root.InvokeTreeNodeCreated(this);
    }

    /// <summary>
    ///     Represents a single node in a tree which contains a data payload
    /// </summary>
    public interface IAppTreeNode<TDetailPayload> where TDetailPayload : new()
    {
        /// <summary>
        ///     Node data detail
        /// </summary>
        Versionable<TDetailPayload> Detail { get; set; }
    }
}
