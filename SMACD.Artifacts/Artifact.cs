using SMACD.Artifacts.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SMACD.Artifacts
{
    public abstract class Artifact
    {
        /// <summary>
        /// Parent Artifact
        /// </summary>
        public Artifact Parent { get; set; }
        internal ObservableCollection<Artifact> Children { get; } = new ObservableCollection<Artifact>();

        /// <summary>
        /// Artifact Identifier for path
        /// </summary>
        public abstract string Identifier { get; }

        /// <summary>
        /// Data attachments to Artifact
        /// </summary>
        public DataArtifactCollection Attachments { get; set; }

        /// <summary>
        /// Vulnerabilities related to Artifact
        /// </summary>
        public List<Vulnerability> Vulnerabilities { get; } = new List<Vulnerability>();

        /// <summary>
        /// Names of children in index
        /// </summary>
        public List<string> ChildNames => Children.Select(c => c.Identifier).ToList();

        /// <summary>
        /// Get a child Artifact by its identifier
        /// </summary>
        /// <param name="identifier">Artifact identifier, unique to this parent</param>
        /// <returns></returns>
        public Artifact GetChildById(string identifier)
        {
            return Children.FirstOrDefault(c => c.Identifier == identifier);
        }

        private bool firingEvents = false;
        internal void BeginFiringEvents()
        {
            firingEvents = true;
        }

        protected Artifact()
        {
            if (!(this is DataArtifactCollection))
            {
                Attachments = new DataArtifactCollection() { Parent = this };
            }

            NotifyCreated();
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

        /// <summary>
        /// Disconnect parents in Artifact tree
        /// </summary>
        public void Disconnect()
        {
            Parent = null;
            foreach (var child in Children)
                child.Disconnect();

            if (Attachments.ChildNames.Count == 0)
                Attachments = null;
            else
            {
                Attachments.Parent = null;
                foreach (var child in Attachments.Children)
                    child.Disconnect();
            }
        }

        /// <summary>
        /// Create Parent pointers in Artifact tree
        /// </summary>
        public void Connect(Artifact parent = null)
        {
            Parent = parent;
            foreach (var child in Children)
                child.Connect(this);

            if (Attachments == null) Attachments = new DataArtifactCollection() { Parent = this };
            Attachments.Parent = this;
            foreach (var item in Attachments.ChildNames)
                Attachments[item].Connect(this);
        }

        /// <summary>
        /// Get a List of Artifacts that represent the path back to the root element, root-first
        /// </summary>
        /// <param name="path">Artifact path built so far</param>
        /// <returns></returns>
        public List<Artifact> GetPathToRoot(List<Artifact> path = null)
        {
            if (path == null)
            {
                path = new List<Artifact>();
            }

            path.Add(this);
            if (Parent == null)
            {
                path.Reverse();
                return path;
            }
            else
            {
                return Parent.GetPathToRoot(path);
            }
        }

        protected void NotifyChanged()
        {
            if (!firingEvents)
            {
                return;
            }

            if (!(this is RootArtifact) && Parent == null)
            {
                return;
            }

            List<Artifact> path = GetPathToRoot();
            RootArtifact root = (RootArtifact)path.First();
            root.InvokeArtifactChanged(path.Last(), path);
        }
        protected void NotifyChildAdded(Artifact newChild)
        {
            if (!firingEvents)
            {
                return;
            }

            if (!(this is RootArtifact) && Parent == null)
            {
                return;
            }

            List<Artifact> path = newChild.GetPathToRoot();
            RootArtifact root = (RootArtifact)path.First();
            root.InvokeArtifactChildAdded(newChild, path);
        }
        protected void NotifyCreated()
        {
            if (!firingEvents)
            {
                return;
            }

            if (!(this is RootArtifact) && Parent == null)
            {
                return;
            }

            List<Artifact> path = GetPathToRoot();
            RootArtifact root = (RootArtifact)path.First();
            root.InvokeArtifactCreated(path.Last(), path);
        }
    }
}
