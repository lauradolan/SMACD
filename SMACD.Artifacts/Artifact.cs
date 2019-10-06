using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SMACD.Artifacts.Data;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents a single node in an Artifact correlation tree, including its metadata and evidence
    /// </summary>
    public abstract class Artifact
    {
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
        public string NiceIdentifier => Identifiers.FirstOrDefault(i => !Guid.TryParse(i, out var dummy));

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
                Identifiers.Add(UUID.ToString());

            NotifyCreated();
            Children.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (var item in e.NewItems)
                        NotifyChildAdded((Artifact)item);
            };
        }

        internal void BeginFiringEvents()
        {
            firingEvents = true;
        }

        /// <summary>
        ///     Disconnect parents in Artifact tree
        /// </summary>
        public virtual void Disconnect()
        {
            return; // TODO -- this might break everything -- ###################################&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&#############################
            Parent = null;
            foreach (var child in Children)
                child.Disconnect();
        }

        /// <summary>
        ///     Create Parent pointers in Artifact tree
        /// </summary>
        public virtual void Connect(Artifact parent = null)
        {
            return; // TODO -- this might break everything -- ###################################&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&#############################
            Parent = parent;
            foreach (var child in Children)
                child.Connect(this);

            if (Attachments == null) Attachments = new DataArtifactCollection();
        }

        /// <summary>
        ///     Returns the leaf Artifact associated with a given UUID path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Artifact GetNodeByRelativeUUIDPath(string path)
        {
            var pathElements = path.Split(PATH_SEPARATOR).Where(p => p != null).ToList();
            if (pathElements.First() == this.UUID.ToString())
                pathElements.RemoveAt(0);

            var node = this;
            foreach (var element in pathElements)
            {
                var uuid = Guid.Parse(element);
                node = node.Children.FirstOrDefault(c => c.UUID == uuid);
                if (node == null)
                    throw new Exception($"Path not valid. Element '{element}' failed");
            }
            return node;
        }

        /// <summary>
        ///     Get the string representing each element from here to the root, using their identifier text
        /// </summary>
        /// <returns>String representing each element from here to the root, using their identifier text</returns>
        public string GetDisplayPathToRoot() => string.Join(PATH_SEPARATOR, GetNodesToRoot().Select(n => n.Identifiers.First()));

        /// <summary>
        ///     Get the string representing each element from here to the root, using their UUID
        /// </summary>
        /// <returns></returns>
        public string GetUUIDPathToRoot() => string.Join(PATH_SEPARATOR, GetNodesToRoot().Select(n => n.UUID.ToString()));

        /// <summary>
        ///     Get a list of nodes between this node and the root node
        /// </summary>
        /// <returns></returns>
        public List<Artifact> GetNodesToRoot()
        {
            var nodes = new List<Artifact>();
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
        ///     Notify root element to fire an ArtifactChanged event
        /// </summary>
        protected void NotifyChanged() => InvokeOnRoot((path, root, leaf) => root.InvokeArtifactChanged(leaf, path));

        /// <summary>
        ///     Notify root element to fire an ArtifactChildAdded event
        /// </summary>
        /// <param name="newChild">Child added</param>
        protected void NotifyChildAdded(Artifact newChild) => InvokeOnRoot((path, root, leaf) => root.InvokeArtifactChildAdded(newChild, path));

        /// <summary>
        ///     Notify root element to fire an ArtifactCreated event
        /// </summary>
        protected void NotifyCreated() => InvokeOnRoot((path, root, leaf) => root.InvokeArtifactCreated(leaf, path));

        private void InvokeOnRoot(Action<List<Artifact>, RootArtifact, Artifact> action)
        {
            if (!firingEvents) return;
            if (!(this is RootArtifact) && Parent == null) return;

            var path = GetNodesToRoot();
            action(path, path.First() as RootArtifact, path.Last());
        }
    }
}