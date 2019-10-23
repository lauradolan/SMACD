using System;

namespace SMACD.AppTree.Evidence
{
    /// <summary>
    ///     Represents Evidence that provides a system directory for collecting files
    /// </summary>
    public class NativeDirectoryEvidence : Evidence
    {
        private WeakReference<NativeDirectoryContext> _contextRef;

        /// <summary>
        ///     Represents Evidence that provides a system directory for collecting files
        /// </summary>
        /// <param name="name">Evidence name</param>
        public NativeDirectoryEvidence(string name) : base(name)
        {
        }

        /// <summary>
        ///     If the Evidence has dispatched a Context
        /// </summary>
        public bool HasActiveDispatchedContext => _contextRef != null && _contextRef.TryGetTarget(out NativeDirectoryContext dummy);

        /// <summary>
        ///     Get a directory Context to collect files locally; when the context is disposed, the directory will be
        ///     automatically ZIPped and saved to the Evidence's StoredData buffer
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