using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Extensions to parse string Paths into Nodes
    /// </summary>
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

        /// <summary>
        ///     If a Node meets the constraints of a provided path segment
        /// </summary>
        /// <param name="pathSegment">Path segment</param>
        /// <param name="node">Node to test</param>
        /// <returns></returns>
        public static bool NodeMeetsConstraints(this string pathSegment, AppTreeNode node) =>
            (HasTypeConstraint(pathSegment) ? NodeMeetsTypeConstraint(pathSegment, node) : true) &&
            (NodeMeetsNameConstraint(GetName(pathSegment), node));

        /// <summary>
        ///     Get a Node Path by its string path, provided a Root node
        /// </summary>
        /// <param name="path">Path to node(s)</param>
        /// <param name="root">Root node to search from</param>
        /// <returns></returns>
        public static PathGeneration GetNodeByPath(this string path, RootNode root)
        {
            return GetGeneration(root, path.Split(PATH_SEPARATOR).Where(p => !string.IsNullOrEmpty(p)).ToList(), 0);
        }
        private static PathGeneration GetGeneration(AppTreeNode node, List<string> segments, int index)
        {
            var generation = new PathGeneration()
            {
                MatchingNode = node,
                Path = string.Join(PATH_SEPARATOR, segments),
                PathSegmentIndex = index
            };

            var nextIndex = 0;
            if (HasAnyConstraint(segments[index]) && NodeMeetsConstraints(segments[index], node))
            {
                // has constraints which match, advance to children (won't be wildcard here)
                nextIndex = index + 1;
            }
            if (index < segments.Count && !HasAnyConstraint(segments[index]) && HasAnyConstraint(segments[index + 1]) && NodeMeetsConstraints(segments[index + 1], node))
            {
                // transitioning from ** to constrained (and met)
                nextIndex = index + 1;
            }
            else if (!HasAnyConstraint(segments[index]))
            {
                // wildcard, don't advance ptr
                nextIndex = index;
            }

            if (nextIndex == segments.Count && NodeMeetsConstraints(segments[index], node))
            {
                // no more path segments after this, if constraints are met this is successful
                generation.IsResultNode = true;
            }
            else if (!HasAnyConstraint(segments[index]) && nextIndex == segments.Count - 1 && NodeMeetsConstraints(segments[index + 1], node))
            {
                // currently inside wildcard but last (next) path segment matches this node
                generation.IsResultNode = true;
            }

            if (index + 1 == segments.Count)
            {
                if (generation.IsResultNode)
                    return generation;
                return null;
            }

            var children = node.Children.Where(c => NodeMeetsConstraints(segments[nextIndex], c));
            if (!children.Any() && segments.Count > index)
                return null;

            foreach (var child in children)
            {
                var newGeneration = GetGeneration(child, segments, nextIndex);
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
