using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SMACD.AppTree.Evidence
{
    /// <summary>
    ///     Contains items which represent evidence supporting correlations or other decisions
    /// </summary>
    public class EvidenceCollection : List<Evidence>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        ///     Retrieve Evidence item by its name
        /// </summary>
        /// <param name="name">Evidence name</param>
        /// <returns></returns>
        public Evidence this[string name] => this.FirstOrDefault(c => c.Name == name);

        /// <summary>
        ///     Fired when Evidence is created
        /// </summary>
        public static event EventHandler<Evidence> EvidenceCreated;

        /// <summary>
        ///     Fired when Evidence is changed
        /// </summary>
        public static event EventHandler<Evidence> EvidenceChanged;

        /// <summary>
        ///     Create or load a native (system) path to allow external tools to put data into the system
        ///     via a local path. This is zipped when the underlying context is disposed.
        ///     Only one context can be opened at a time, but this method will allow the caller to specify
        ///     (if desired) a duration, in seconds, to wait until the resource becomes available.
        /// </summary>
        /// <param name="name">Evidence name</param>
        /// <param name="availabilityWaitTimeout">Time to wait if the context is already opened</param>
        /// <returns></returns>
        public NativeDirectoryEvidence CreateOrLoadNativePath(string name, TimeSpan availabilityWaitTimeout = default)
        {
            _lock.EnterReadLock();
            Evidence existingChild = this[name];
            if (existingChild != null && !(existingChild is NativeDirectoryEvidence))
            {
                throw new Exception("Native Directory handle cannot be replaced/overwritten by another storage type");
            }

            _lock.ExitReadLock();

            if (availabilityWaitTimeout == default)
            {
                availabilityWaitTimeout = TimeSpan.FromSeconds(0);
            }

            if (!_lock.TryEnterWriteLock(availabilityWaitTimeout))
            {
                throw new Exception("Native Directory handle has an active context for this identifier" +
                                    (availabilityWaitTimeout != default
                                        ? " and timeout of " + availabilityWaitTimeout + " expired"
                                        : ""));
            }

            NativeDirectoryEvidence artifact = null;
            if (existingChild != null)
            {
                artifact = (NativeDirectoryEvidence)existingChild;
                EvidenceChanged?.Invoke(this, artifact);
            }
            else
            {
                artifact = new NativeDirectoryEvidence(name);
                Add(artifact);
                EvidenceCreated?.Invoke(this, artifact);
            }

            _lock.ExitWriteLock();
            return artifact;
        }

        /// <summary>
        ///     Create a child Evidence item containing a serialized object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="name">Artifact name</param>
        /// <param name="obj">Object to save</param>
        /// <returns></returns>
        public ObjectEvidence Save<T>(string name, T obj)
        {
            ObjectEvidence newChild = new ObjectEvidence(name);
            newChild.Set(obj);
            return Save(newChild) as ObjectEvidence;
        }

        /// <summary>
        ///     Create a child Evidence item containing a string
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="str">String to save</param>
        /// <returns></returns>
        public ObjectEvidence Save(string name, string str)
        {
            return Save(GetNewArtifact(name, str)) as ObjectEvidence;
        }

        /// <summary>
        ///     Create a child Evidence item containing a byte array
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <param name="data">Byte array to save</param>
        /// <returns></returns>
        public Evidence Save(string name, byte[] data)
        {
            return Save(GetNewArtifact(name, data));
        }

        /// <summary>
        ///     Create a blank child Evidence item
        /// </summary>
        /// <param name="name">Artifact name</param>
        /// <returns></returns>
        public Evidence Save(string name)
        {
            return Save(GetNewArtifact(name, new byte[0]));
        }

        private Evidence Save(Evidence newChild)
        {
            _lock.EnterWriteLock();

            Evidence existingChild = this[newChild.Name];
            if (existingChild != null)
            {
                Remove(existingChild);
                EvidenceChanged?.Invoke(this, existingChild);
            }

            Add(newChild);
            _lock.ExitWriteLock();

            if (existingChild == null)
            {
                EvidenceCreated?.Invoke(this, newChild);
            }

            return newChild;
        }

        private Evidence GetNewArtifact(string name, byte[] data)
        {
            return new Evidence(name)
            {
                StoredData = data
            };
        }

        private StringEvidence GetNewArtifact(string name, string data)
        {
            return new StringEvidence(name)
            {
                StoredData = Encoding.UTF8.GetBytes(data)
            };
        }
    }
}