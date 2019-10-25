using System;
using System.IO;
using System.IO.Compression;

namespace SMACD.AppTree.Evidence
{
    /// <summary>
    ///     Context which allocates and destroys a temporary directory to collect files for a NativeDirectoryEvidence
    /// </summary>
    public class NativeDirectoryContext : IDisposable
    {
        /// <summary>
        ///     Local path to use when extracting/accessing NativeDirectoryContexts.
        ///     This defaults to the temp path provided by the environment.
        /// </summary>
        public static string TemporaryWorkingPath { get; set; } = Path.GetTempPath();

        /// <summary>
        ///     Open a previously created NativeDirectoryContext from its stored state
        /// </summary>
        /// <param name="data">Data buffer to load</param>
        internal NativeDirectoryContext(byte[] data) : this()
        {
            StoredData = data;
            if (StoredData != null && StoredData.Length > 0)
            {
                string zipFile = Path.GetTempFileName();
                File.WriteAllBytes(zipFile, StoredData);
                ZipFile.ExtractToDirectory(zipFile, Directory);
                File.Delete(zipFile);
            }
        }

        /// <summary>
        ///     Create a new NativeDirectoryContext to store files
        /// </summary>
        internal NativeDirectoryContext()
        {
            Directory = Path.Combine(TemporaryWorkingPath, "wks", new Random((int)DateTime.Now.Ticks).Next(1024, int.MaxValue).ToString());
            System.IO.Directory.CreateDirectory(Directory);
        }

        internal byte[] StoredData { get; private set; } = new byte[0];

        /// <summary>
        ///     Directory path allocated by this Context to store files and directories
        ///     in; contents will be compressed in full upon Context disposal
        /// </summary>
        public string Directory { get; }

        internal event EventHandler Disposing;

        /// <summary>
        ///     Directory path and a filename concatenated together
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public string DirectoryWithFile(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }

        private bool IsDirectoryWritable(string path)
        {
            try
            {
                using (FileStream fs = File.Create(Path.Combine(path, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
                {
                    // do nothing on success
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        /// <summary>
        ///     Dispose of context
        /// </summary>
        /// <param name="disposing">Currently disposing?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                string target = Path.GetTempFileName();
                File.Delete(target);
                ZipFile.CreateFromDirectory(Directory, target);
                StoredData = File.ReadAllBytes(target);
                File.Delete(target);
                Disposing?.Invoke(this, new EventArgs());
                disposedValue = true;
            }
        }

        /// <summary>
        ///     Destructor to dispose
        /// </summary>
        ~NativeDirectoryContext()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Dispose of context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}