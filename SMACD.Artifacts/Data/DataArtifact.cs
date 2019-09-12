namespace SMACD.Artifacts.Data
{
    public class DataArtifact
    {
        public DataArtifact(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Name of this Artifact
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Data stored in this Artifact in raw format
        /// </summary>
        public byte[] StoredData { get; set; }

        /// <summary>
        ///     Get the Artifact cast as a specific DataArtifact type
        /// </summary>
        /// <typeparam name="T">Artifact type</typeparam>
        /// <returns></returns>
        public T As<T>() where T : DataArtifact
        {
            return (T) this;
        }

        /// <summary>
        ///     Get this as a string-containing Artifact
        /// </summary>
        /// <returns></returns>
        public StringArtifact AsStringArtifact()
        {
            return As<StringArtifact>();
        }

        /// <summary>
        ///     Get this as an object-containing Artifact
        /// </summary>
        /// <returns></returns>
        public ObjectArtifact AsObjectArtifact()
        {
            return As<ObjectArtifact>();
        }

        /// <summary>
        ///     Get this as a native directory-providing Artifact
        /// </summary>
        /// <returns></returns>
        public NativeDirectoryArtifact AsNativeDirectoryArtifact()
        {
            return As<NativeDirectoryArtifact>();
        }
    }
}