using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SMACD.Artifacts.Data
{
    public class DataArtifactCollection : List<DataArtifact>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Fired when Artifact is created
        /// </summary>
        public static event EventHandler<DataArtifact> ArtifactCreated;

        /// <summary>
        /// Fired when Artifact is changed
        /// </summary>
        public static event EventHandler<DataArtifact> ArtifactChanged;

        /// <summary>
        /// Retrieve DataArtifact by its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataArtifact this[string name] => this.FirstOrDefault(c => c.Name == name);

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
            var existingChild = this[name];
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
                ArtifactChanged?.Invoke(this, artifact);
            }
            else
            {
                artifact = new NativeDirectoryArtifact(name);
                this.Add(artifact);
                ArtifactCreated?.Invoke(this, artifact);
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
            ObjectArtifact newChild = new ObjectArtifact(name);
            newChild.Set(obj);
            return Save(newChild) as ObjectArtifact;
        }

        /// <summary>
        /// Create a child Artifact containing a string
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="str">String to save</param>
        /// <returns></returns>
        public StringArtifact Save(string name, string str) => Save(GetNewArtifact(name, str)) as StringArtifact;

        /// <summary>
        /// Create a child Artifact containing a byte array
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="data">Byte array to save</param>
        /// <returns></returns>
        public DataArtifact Save(string name, byte[] data) => Save(GetNewArtifact(name, data));

        /// <summary>
        /// Create a blank child Artifact
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <returns></returns>
        public DataArtifact Save(string name) => Save(GetNewArtifact(name, new byte[0]));

        private DataArtifact Save(DataArtifact newChild)
        {
            _lock.EnterWriteLock();

            DataArtifact existingChild = this[newChild.Name];
            if (existingChild != null)
            {
                Remove(existingChild);
                ArtifactChanged?.Invoke(this, existingChild);
            }
            Add(newChild);
            _lock.ExitWriteLock();

            if (existingChild == null)
            {
                ArtifactCreated?.Invoke(this, newChild);
            }
            return newChild;
        }

        private DataArtifact GetNewArtifact(string name, byte[] data)
        {
            return new DataArtifact(name)
            {
                StoredData = data
            };
        }

        private StringArtifact GetNewArtifact(string name, string data)
        {
            return new StringArtifact(name)
            {
                StoredData = Encoding.UTF8.GetBytes(data)
            };
        }
    }
}
