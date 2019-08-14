using System;

namespace SMACD.Artifacts.Data
{
    /// <summary>
    /// Represents an Artifact that provides a system directory for collecting files
    /// </summary>
    public class NativeDirectoryArtifact : DataArtifact
    {
        /// <summary>
        /// If the Artifact has dispatched a Context
        /// </summary>
        public bool HasActiveDispatchedContext => _contextRef != null && _contextRef.TryGetTarget(out NativeDirectoryContext dummy);
        private WeakReference<NativeDirectoryContext> _contextRef;

        public NativeDirectoryArtifact(string name) : base(name) { }

        /// <summary>
        /// Get a directory Context to collect files locally; when the context is disposed, the directory will be
        ///   automatically ZIPped and saved to the Artifact's StoredData buffer
        /// </summary>
        /// <returns></returns>
        public NativeDirectoryContext GetContext()
        {
            NativeDirectoryContext context = new NativeDirectoryContext(StoredData);
            context.Disposing += (s, e) =>
            {
                _contextRef = null;
                StoredData = context.StoredData;
            };
            _contextRef = new WeakReference<NativeDirectoryContext>(context);
            return context;
        }
    }
}
