using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace SMACD.Artifacts.Data
{
    public class DataArtifactCollection : Artifact
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Fired when Artifact is created
        /// </summary>
        public static event EventHandler<Artifact> ArtifactCreated;

        /// <summary>
        /// Fired when Artifact is changed
        /// </summary>
        public static event EventHandler<Artifact> ArtifactChanged;

        public override string Identifier => "Data Artifacts";

        public DataArtifact this[string name] => (DataArtifact)Children.FirstOrDefault(c => c.Identifier == name);

        /// <summary>
        /// Create or load a native (system) path to allow external tools to put data into the system
        ///   via a local path. This is zipped when the underlying context is disposed.
        ///   
        /// Only one context can be opened at a time, but this method will allow the caller to specify
        ///   (if desired) a duration, in seconds, to wait until the resource becomes available.
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="availabilityWaitTimeout">Time to wait if the context is already opened</param>
        /// <returns></returns>
        public NativeDirectoryArtifact CreateOrLoadNativePath(string name, TimeSpan availabilityWaitTimeout = default(TimeSpan))
        {
            _lock.EnterReadLock();
            Artifact existingChild = Children.FirstOrDefault(c => c.Identifier == name);
            if (existingChild != null && !(existingChild is NativeDirectoryArtifact))
            {
                throw new Exception("Native Directory handle cannot be replaced/overwritten by another storage type");
            }

            _lock.ExitReadLock();

            if (availabilityWaitTimeout == default(TimeSpan))
            {
                availabilityWaitTimeout = TimeSpan.FromSeconds(0);
            }

            if (!_lock.TryEnterWriteLock(availabilityWaitTimeout))
            {
                throw new Exception("Native Directory handle has an active context for this identifier" + (availabilityWaitTimeout != default(TimeSpan) ? " and timeout of " + availabilityWaitTimeout.ToString() + " expired" : ""));
            }

            NativeDirectoryArtifact artifact = null;
            if (existingChild != null)
            {
                artifact = (NativeDirectoryArtifact)existingChild;
                ArtifactChanged?.Invoke(this, this);
            }
            else
            {
                artifact = new NativeDirectoryArtifact(name)
                {
                    Parent = this
                };

                Children.Add(artifact);
                ArtifactCreated?.Invoke(this, this);
            }

            _lock.ExitWriteLock();
            return artifact;
        }

        /// <summary>
        /// Create a child Artifact containing a serialized object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="name">Artifact name</param>
        /// <param name="obj">Object to save</param>
        /// <returns></returns>
        public ObjectArtifact Save<T>(string name, T obj)
        {
            _lock.EnterWriteLock();
            ObjectArtifact newChild = new ObjectArtifact(name)
            {
                Parent = this
            };
            newChild.Set(obj);

            Artifact existingChild = Children.FirstOrDefault(c => c.Identifier == name);
            if (existingChild != null)
            {
                Children.Remove(existingChild);
                foreach (Artifact child in existingChild.Children)
                {
                    newChild.Children.Add(child);
                }

                ArtifactChanged?.Invoke(this, this);
            }
            Children.Add(newChild);
            _lock.ExitWriteLock();

            if (existingChild == null)
            {
                ArtifactCreated?.Invoke(this, this);
            }

            return newChild;
        }

        /// <summary>
        /// Create a child Artifact containing a string
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="str">String to save</param>
        /// <returns></returns>
        public StringArtifact Save(string name, string str)
        {
            _lock.EnterWriteLock();
            StringArtifact newChild = GetNewArtifact(name, str);
            Artifact existingChild = Children.FirstOrDefault(c => c.Identifier == name);
            if (existingChild != null)
            {
                Children.Remove(existingChild);
                foreach (Artifact child in existingChild.Children)
                {
                    newChild.Children.Add(child);
                }

                ArtifactChanged?.Invoke(this, this);
            }
            Children.Add(newChild);
            _lock.ExitWriteLock();
            if (existingChild == null)
            {
                ArtifactCreated?.Invoke(this, this);
            }

            return newChild;
        }

        /// <summary>
        /// Create a child Artifact containing a byte array
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="data">Byte array to save</param>
        /// <returns></returns>
        public Artifact Save(string name, byte[] data)
        {
            _lock.EnterWriteLock();
            DataArtifact newChild = GetNewArtifact(name, data);
            Artifact existingChild = Children.FirstOrDefault(c => c.Identifier == name);
            if (existingChild != null)
            {
                Children.Remove(existingChild);
                foreach (Artifact child in existingChild.Children)
                {
                    newChild.Children.Add(child);
                }

                ArtifactChanged?.Invoke(this, this);
            }
            Children.Add(newChild);
            _lock.ExitWriteLock();
            if (existingChild == null)
            {
                ArtifactCreated?.Invoke(this, this);
            }

            return newChild;
        }

        /// <summary>
        /// Create a blank child Artifact
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <returns></returns>
        public Artifact Save(string name)
        {
            _lock.EnterWriteLock();
            DataArtifact newChild = GetNewArtifact(name, new byte[0]);
            Artifact existingChild = Children.FirstOrDefault(c => c.Identifier == name);
            if (existingChild != null)
            {
                Children.Remove(existingChild);
                foreach (Artifact child in existingChild.Children)
                {
                    newChild.Children.Add(child);
                }

                ArtifactChanged?.Invoke(this, this);
            }
            Children.Add(newChild);
            _lock.ExitWriteLock();
            if (existingChild == null)
            {
                ArtifactCreated?.Invoke(this, this);
            }

            return newChild;
        }

        private DataArtifact GetNewArtifact(string name, byte[] data)
        {
            return new DataArtifact(name)
            {
                Parent = this,
                StoredData = data
            };
        }

        private StringArtifact GetNewArtifact(string name, string data)
        {
            return new StringArtifact(name)
            {
                Parent = this,
                StoredData = Encoding.UTF8.GetBytes(data)
            };
        }

        private ObjectArtifact GetNewArtifact<T>(string name, T data)
        {
            ObjectArtifact artifact = new ObjectArtifact(name)
            {
                Parent = this
            };
            artifact.Set(data);
            return artifact;
        }
    }
}
