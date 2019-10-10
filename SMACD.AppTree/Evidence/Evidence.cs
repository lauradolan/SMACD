namespace SMACD.AppTree.Evidence
{
    public class Evidence
    {
        /// <summary>
        /// Possible encapsulated data types expressed in Evidence
        /// </summary>
        public enum EvidenceTypes
        {
            Unknown,
            Object,
            String,
            VFS
        }

        /// <summary>
        ///     Wraps supporting evidence data
        /// </summary>
        /// <param name="name">Name of Evidence item</param>
        public Evidence(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Name of this Evidence item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Data stored in this Evidence item in raw format
        /// </summary>
        public byte[] StoredData { get; set; }

        /// <summary>
        /// Type of Evidence item
        /// </summary>
        public EvidenceTypes EvidenceType
        {
            get
            {
                if (this is ObjectEvidence)
                {
                    return EvidenceTypes.Object;
                }

                if (this is StringEvidence)
                {
                    return EvidenceTypes.String;
                }

                if (this is NativeDirectoryEvidence)
                {
                    return EvidenceTypes.VFS;
                }

                return EvidenceTypes.Unknown;
            }
        }

        /// <summary>
        ///     Get the Evidence cast as a specific Evidence type
        /// </summary>
        /// <typeparam name="T">Artifact type</typeparam>
        /// <returns></returns>
        public T As<T>() where T : Evidence
        {
            return (T)this;
        }

        /// <summary>
        ///     Get this as a string-containing Artifact
        /// </summary>
        /// <returns></returns>
        public StringEvidence AsStringEvidence()
        {
            return As<StringEvidence>();
        }

        /// <summary>
        ///     Get this as an object-containing Artifact
        /// </summary>
        /// <returns></returns>
        public ObjectEvidence AsObjectEvidence()
        {
            return As<ObjectEvidence>();
        }

        /// <summary>
        ///     Get this as a native directory-providing Artifact
        /// </summary>
        /// <returns></returns>
        public NativeDirectoryEvidence AsNativeDirectoryEvidence()
        {
            return As<NativeDirectoryEvidence>();
        }
    }
}