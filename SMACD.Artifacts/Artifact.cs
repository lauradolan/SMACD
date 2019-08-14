using SMACD.Artifacts.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SMACD.Artifacts
{
    public abstract class Artifact
    {
        public Artifact Parent { get; set; }
        internal ObservableCollection<Artifact> Children { get; } = new ObservableCollection<Artifact>();
        public abstract string Identifier { get; }

        public DataArtifactCollection Attachments { get; set; }
        public List<Vulnerability> Vulnerabilities { get; } = new List<Vulnerability>();

        public List<string> ChildNames => Children.Select(c => c.Identifier).ToList();
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
                Attachments = new DataArtifactCollection();
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

        public void Disconnect()
        {
            Parent = null;
            foreach (var child in Children)
                child.Disconnect();
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
    }
}
