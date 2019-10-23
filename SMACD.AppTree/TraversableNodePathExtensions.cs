using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMACD.AppTree
{
    public class PathGeneration
    {
        public string Path { get; set; }
        public int PathSegmentIndex { get; set; }
        public string PathSegment => Path.Split(PathParserExtensions.PATH_SEPARATOR)[PathSegmentIndex];

        public AppTreeNode MatchingNode { get; set; }
        public List<PathGeneration> Children { get; set; } = new List<PathGeneration>();
    }

    public static class PathParserExtensions
    {
        internal const string PATH_SEPARATOR = "//";
        private const string SINGLE_GENERATION_WILDCARD = "*";
        private const string N_GENERATION_WILDCARD = "**";
        private static readonly string[] CONTROL_CHARS = new string[] { SINGLE_GENERATION_WILDCARD, N_GENERATION_WILDCARD };

        private static Regex TypeConstraintRegex { get; } = new Regex("\\{(?<type>\\S+)\\}(?<name>\\S+)");
        private static Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();

        private static bool HasAnyConstraint(this string pathSegment) =>
            HasTypeConstraint(pathSegment) ||
            HasNameConstraint(HasTypeConstraint(pathSegment) ? GetNameWithoutTypeConstraint(pathSegment) : pathSegment);
        private static bool HasNameConstraint(this string pathSegment) => !CONTROL_CHARS.Contains(pathSegment);
        private static bool HasTypeConstraint(this string pathSegment) => TypeConstraintRegex.IsMatch(pathSegment);
        private static Type GetTypeConstraint(this string pathSegment) => HasTypeConstraint(pathSegment) ? Lookup(TypeConstraintRegex.Match(pathSegment).Groups["type"].Value) : null;
        private static string GetName(this string pathSegment) => HasTypeConstraint(pathSegment) ? GetNameWithoutTypeConstraint(pathSegment) : pathSegment;
        private static string GetNameWithoutTypeConstraint(this string pathSegment) => HasTypeConstraint(pathSegment) ? TypeConstraintRegex.Match(pathSegment).Groups["name"].Value : pathSegment;
        private static bool NodeMeetsTypeConstraint(this string pathSegment, AppTreeNode node) => GetTypeConstraint(pathSegment).IsAssignableFrom(node.GetType());

        private static bool NodeMeetsNameConstraint(this string pathSegment, AppTreeNode node)
        {
            if (!CONTROL_CHARS.Contains(pathSegment))
            {
                // Convert to Regex to support ?/* (1-char/n-char) wildcards
                string regexTest = "^" + Regex.Escape(pathSegment).Replace("\\?", ".").Replace("\\*", ".*") + "$";
                return node.Identifiers.Any(i => Regex.IsMatch(i, regexTest));
            }
            return true; // must be a wildcard
        }

        public static bool NodeMeetsConstraints(this string pathSegment, AppTreeNode node) =>
            (HasTypeConstraint(pathSegment) ? NodeMeetsTypeConstraint(pathSegment, node) : true) &&
            (NodeMeetsNameConstraint(GetName(pathSegment), node));

        public static PathGeneration GetNodeByPath(this string path, RootNode root)
        {
            return GetGeneration(root, path.Split(PATH_SEPARATOR).ToList(), 0);
        }
        private static PathGeneration GetGeneration(AppTreeNode node, List<string> segments, int index)
        {
            int newIndex = index;
            if (HasAnyConstraint(segments[index]) || NodeMeetsConstraints(segments[index+1], node))
                newIndex = index + 1;
            else if (GetNameWithoutTypeConstraint(segments[index]) == N_GENERATION_WILDCARD)
                newIndex = index;

            // todo: how to handle when **//{UrlNode}* has {UrlNode} underneath? (i.e. ** is preferred when specific matches better)
            // -- do it by length? ... if there are more nodes to process than there are path elements remaining then prefer **?

            // host//service//urlnode//urlnode//urlrequest
            // **//{UrlNode}*

            // host ** // service ** // urlnode {UrlNode}* // urlnode ...?
            // if moving from ** to specific, traverse into the next node once for itself, once as **

            if (newIndex > segments.Count-1)
                return null;

            var generation = new PathGeneration()
            {
                MatchingNode = node,
                Path = string.Join(PATH_SEPARATOR, segments),
                PathSegmentIndex = newIndex
            };

            var children = node.Children.Where(c => NodeMeetsConstraints(segments[newIndex], c));
            if (!children.Any() && segments.Count > index)
                return null;

            foreach (var child in children)
            {
                var newGeneration = GetGeneration(child, segments, newIndex);
                if (newGeneration != null)
                    generation.Children.Add(newGeneration);
            }
            return generation;
        }

        private static Type Lookup(string type)
        {
            if (!_cachedTypes.ContainsKey(type))
            {
                System.Reflection.Assembly asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.DefinedTypes.Any(t => t.Name == type || t.FullName == type));
                if (asm == null)
                {
                    throw new Exception($"Trigger Path required type safety check against expected type {type}, which does not exist");
                }
                _cachedTypes.Add(type, asm.DefinedTypes.First(t => t.Name == type || t.FullName == type).UnderlyingSystemType);
            }
            return _cachedTypes[type];
        }
    }
}
