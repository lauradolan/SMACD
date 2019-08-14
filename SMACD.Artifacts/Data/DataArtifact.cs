using System;
using System.Linq;
using System.Threading;

namespace SMACD.Artifacts.Data
{
    public class DataArtifact : Artifact
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static Func<string, Type> ResolveType { get; set; } = new Func<string, Type>((s) => null);

        public override string Identifier => Name;

        /// <summary>
        /// Name of this Artifact
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data stored in this Artifact in raw format
        /// </summary>
        public byte[] StoredData { get; set; }

        /// <summary>
        /// Get a child Artifact
        /// </summary>
        /// <param name="artifactName">Artifact name</param>
        /// <returns></returns>
        public DataArtifact this[string artifactName]
        {
            get
            {
                _lock.EnterUpgradeableReadLock();
                Artifact item = Children.FirstOrDefault(c => c.Identifier == artifactName);
                if (item == null)
                {
                    return new DataArtifact(artifactName);
                }

                _lock.ExitUpgradeableReadLock();
                return (DataArtifact)item;
            }
        }

        public DataArtifact(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Get the Artifact cast as a specific Artifact type
        /// </summary>
        /// <typeparam name="T">Artifact type</typeparam>
        /// <returns></returns>
        public T As<T>() where T : DataArtifact
        {
            return (T)this;
        }

        /// <summary>
        /// Get this as a string-containing Artifact
        /// </summary>
        /// <returns></returns>
        public StringArtifact AsStringArtifact()
        {
            return As<StringArtifact>();
        }

        /// <summary>
        /// Get this as an object-containing Artifact
        /// </summary>
        /// <returns></returns>
        public ObjectArtifact AsObjectArtifact()
        {
            return As<ObjectArtifact>();
        }

        /// <summary>
        /// Get this as a native directory-providing Artifact
        /// </summary>
        /// <returns></returns>
        public NativeDirectoryArtifact AsNativeDirectoryArtifact()
        {
            return As<NativeDirectoryArtifact>();
        }
    }
}
