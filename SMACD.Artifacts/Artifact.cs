using SMACD.Artifacts.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents a single node in an Artifact correlation tree, including its metadata and evidence
    /// </summary>
    public abstract class Artifact
    {
        public const string SINGLE_GENERATION_WILDCARD = "*";
        public const string N_GENERATION_WILDCARD = "**";
        public static string[] CONTROL_CHARS = new string[] { SINGLE_GENERATION_WILDCARD, N_GENERATION_WILDCARD };

        public const string PATH_SEPARATOR = "//";
        private bool firingEvents;

        /// <summary>
        ///     Parent Artifact
        /// </summary>
        public Artifact Parent { get; set; }

        /// <summary>
        ///     Children of Artifact
        /// </summary>
        public ObservableCollection<Artifact> Children { get; set; } = new ObservableCollection<Artifact>();

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
        public DataArtifactCollection Attachments { get; set; } = new DataArtifactCollection();

        /// <summary>
        ///     Vulnerabilities related to Artifact
        /// </summary>
        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        /// <summary>
        ///     Get a child Artifact by its identifier
        /// </summary>
        /// <param name="uuid">Artifact UUID</param>
        /// <returns></returns>
        public Artifact this[Guid uuid] => Children.FirstOrDefault(c => c.UUID == uuid);

        /// <summary>
        ///     An Action which can be registered by the Extension to return an HTML component to view artifact
        /// </summary>
        public virtual string ArtifactSummaryViewTypeName { get; } = null;

        /// <summary>
        ///     Represents a single node in an Artifact correlation tree, including its metadata and evidence
        /// </summary>
        protected Artifact()
        {
            if (!Identifiers.Contains(UUID.ToString()))
            {
                Identifiers.Add(UUID.ToString());
            }

            Children.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (object item in e.NewItems)
                    {
                        NotifyChildAdded((Artifact)item);
                    }
                }
            };
        }

        internal void BeginFiringEvents()
        {
            firingEvents = true;
            NotifyCreated(); // deferred call to this to ensure events are wired up first
        }

        /// <summary>
        ///     Create Parent pointers in Artifact tree
        /// </summary>
        public virtual void Connect(Artifact parent = null)
        {
            Parent = parent;
            foreach (Artifact child in Children)
            {
                child.Connect(this);
            }

            if (Attachments == null)
            {
                Attachments = new DataArtifactCollection();
            }
        }

        /// <summary>
        ///     Returns the leaf Artifact associated with a given UUID path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Artifact GetNodeByRelativeUUIDPath(string path)
        {
            List<string> pathElements = path.Split(PATH_SEPARATOR).Where(p => p != null).ToList();
            if (pathElements.First() == UUID.ToString())
            {
                pathElements.RemoveAt(0);
            }

            Artifact node = this;
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
        ///     Get the string representing each element from here to the root, using their identifier text
        /// </summary>
        /// <returns>String representing each element from here to the root, using their identifier text</returns>
        public string GetDisplayPathToRoot()
        {
            return string.Join(PATH_SEPARATOR, GetNodesToRoot().Select(n => n.Identifiers.First()));
        }

        /// <summary>
        ///     Get the string representing each element from here to the root, using their UUID
        /// </summary>
        /// <returns></returns>
        public string GetUUIDPathToRoot()
        {
            return string.Join(PATH_SEPARATOR, GetNodesToRoot().Select(n => n.UUID.ToString()));
        }

        /// <summary>
        ///     Get a list of nodes between this node and the root node
        /// </summary>
        /// <returns></returns>
        public List<Artifact> GetNodesToRoot()
        {
            List<Artifact> nodes = new List<Artifact>();
            Artifact node = this;
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
            var pathSegments = path.Split(Artifact.PATH_SEPARATOR);
            var artifactPathToRoot = GetNodesToRoot();
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

        private static bool? MeetsTypeConstraints(Artifact element, string thisPathSegment, out string newTestString)
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

        private static bool? MeetsNamingConstraints(Artifact element, string thisPathSegment)
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
        protected void NotifyChanged()
        {
            InvokeOnRoot((path, root, leaf) => root.InvokeArtifactChanged(leaf, path));
        }

        /// <summary>
        ///     Notify root element to fire an ArtifactChildAdded event
        /// </summary>
        /// <param name="newChild">Child added</param>
        protected void NotifyChildAdded(Artifact newChild)
        {
            InvokeOnRoot((path, root, leaf) => root.InvokeArtifactChildAdded(newChild, path));
        }

        /// <summary>
        ///     Notify root element to fire an ArtifactCreated event
        /// </summary>
        protected void NotifyCreated()
        {
            InvokeOnRoot((path, root, leaf) => root.InvokeArtifactCreated(leaf, path));
        }

        private void InvokeOnRoot(Action<List<Artifact>, RootArtifact, Artifact> action)
        {
            if (!firingEvents)
            {
                return;
            }

            if (!(this is RootArtifact) && Parent == null)
            {
                return;
            }

            List<Artifact> path = GetNodesToRoot();
            action(path, path.First() as RootArtifact, path.Last());
        }
    }
}