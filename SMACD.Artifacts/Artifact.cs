using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SMACD.Artifacts.Data;

namespace SMACD.Artifacts
{
    public abstract class Artifact
    {
        private bool firingEvents;

        protected Artifact()
        {
            NotifyCreated();
            Children.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (var item in e.NewItems)
                        NotifyChildAdded((Artifact) item);
            };
        }

        /// <summary>
        ///     Parent Artifact
        /// </summary>
        public Artifact Parent { get; set; }

        public ObservableCollection<Artifact> Children { get; set; } = new ObservableCollection<Artifact>();

        /// <summary>
        ///     Artifact Identifier for path
        /// </summary>
        public abstract string Identifier { get; }

        /// <summary>
        ///     Data attachments to Artifact
        /// </summary>
        public DataArtifactCollection Attachments { get; set; } = new DataArtifactCollection();

        /// <summary>
        ///     Vulnerabilities related to Artifact
        /// </summary>
        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        /// <summary>
        ///     Get a child Artifact by its identifier
        /// </summary>
        /// <param name="identifier">Artifact identifier, unique to this parent</param>
        /// <returns></returns>
        public Artifact GetChildById(string identifier)
        {
            return Children.FirstOrDefault(c => c.Identifier == identifier);
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
            Parent = null;
            foreach (var child in Children)
                child.Disconnect();
        }

        /// <summary>
        ///     Create Parent pointers in Artifact tree
        /// </summary>
        public virtual void Connect(Artifact parent = null)
        {
            Parent = parent;
            foreach (var child in Children)
                child.Connect(this);

            if (Attachments == null) Attachments = new DataArtifactCollection();
        }

        /// <summary>
        ///     Get a List of Artifacts that represent the path back to the root element, root-first
        /// </summary>
        /// <param name="path">Artifact path built so far</param>
        /// <returns></returns>
        public List<Artifact> GetPathToRoot(List<Artifact> path = null)
        {
            if (path == null) path = new List<Artifact>();

            path.Add(this);
            if (Parent == null)
            {
                path.Reverse();
                return path;
            }

            return Parent.GetPathToRoot(path);
        }

        protected void NotifyChanged()
        {
            if (!firingEvents) return;

            if (!(this is RootArtifact) && Parent == null) return;

            var path = GetPathToRoot();
            var root = (RootArtifact) path.First();
            root.InvokeArtifactChanged(path.Last(), path);
        }

        protected void NotifyChildAdded(Artifact newChild)
        {
            if (!firingEvents) return;

            if (!(this is RootArtifact) && Parent == null) return;

            var path = newChild.GetPathToRoot();
            var root = (RootArtifact) path.First();
            root.InvokeArtifactChildAdded(newChild, path);
        }

        protected void NotifyCreated()
        {
            if (!firingEvents) return;

            if (!(this is RootArtifact) && Parent == null) return;

            var path = GetPathToRoot();
            var root = (RootArtifact) path.First();
            root.InvokeArtifactCreated(path.Last(), path);
        }
    }
}